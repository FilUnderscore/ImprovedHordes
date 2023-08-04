using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Populator;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.POI
{
    public abstract class WorldZoneHordePopulator<Horde> : HordePopulator<WorldPOIScanner.POIZone> where Horde: IHorde
    {
        private static readonly Setting<ulong> ZONE_HORDE_REPOPULATION_DAYS = new Setting<ulong>("zone_horde_repopulation_days", 7);
        
        private readonly Dictionary<WorldPOIScanner.POIZone, ulong> lastSpawned = new Dictionary<WorldPOIScanner.POIZone, ulong>();

        protected readonly WorldPOIScanner scanner;

        private int MAX_VIEW_DISTANCE
        {
            get
            {
                return WorldHordeTracker.MAX_UNLOAD_VIEW_DISTANCE;
            }
        }

        public WorldZoneHordePopulator(WorldPOIScanner scanner)
        {
            this.scanner = scanner;
        }

        public override bool CanRun(List<PlayerHordeGroup> playerGroups, Dictionary<Type, List<ClusterSnapshot>> clusters)
        {
            return this.scanner.HasScanCompleted();
        }

        public override void Populate(WorldPOIScanner.POIZone zone, WorldHordeSpawner spawner, IWorldRandom worldRandom)
        {
            if (zone != null)
            {
                SpawnHordesAt(zone, spawner, worldRandom);
            }
        }

        public override bool CanPopulate(float dt, out WorldPOIScanner.POIZone zone, List<PlayerHordeGroup> playerGroups, Dictionary<Type, List<ClusterSnapshot>> clusters, IWorldRandom worldRandom)
        {
            WorldPOIScanner.POIZone randomZone = worldRandom.Random<WorldPOIScanner.POIZone>(this.scanner.GetZones());

            if(randomZone.GetDensity() < this.GetMinimumDensity())
            {
                zone = null;
                return false;
            }    

            if (lastSpawned.TryGetValue(randomZone, out ulong spawnTime))
            {
                ulong worldTime = GameManager.Instance.World.worldTime;

                if (worldTime - spawnTime < (24000UL * ZONE_HORDE_REPOPULATION_DAYS.Value))
                {
                    zone = null;
                    return false;
                }
            }

            randomZone.GetLocationOutside(worldRandom, out Vector2 spawnLocation);

            // Check for nearby players.
            foreach(var playerGroup in playerGroups)
            {
                playerGroup.GetPlayerClosestTo(spawnLocation, out float distance);

                if (distance <= MAX_VIEW_DISTANCE)
                {
                    zone = null;
                    return false;
                }
            }

            // Check for nearby hordes.
            foreach (var cluster in clusters[typeof(Horde)])
            {
                if ((randomZone.GetBounds().center - cluster.location).sqrMagnitude <= (randomZone.GetBounds().size.sqrMagnitude / 2))
                {
                    zone = null;
                    return false;
                }
            }

            zone = randomZone;
            return true;
        }

        protected abstract int CalculateHordeCount(WorldPOIScanner.POIZone zone);

        protected virtual bool IsDensityInfluencedByZoneProperties()
        {
            return true;
        }

        protected virtual float GetMinimumDensity()
        {
            return 0.0f;
        }

        private void SpawnHordesAt(WorldPOIScanner.POIZone zone, WorldHordeSpawner spawner, IWorldRandom worldRandom)
        {
            float biomeSparsityFactor = HordeBiomes.DetermineBiomeSparsityFactor(zone.GetCenter());
            int hordeCount = Mathf.CeilToInt(Mathf.Max(1, Mathf.FloorToInt(CalculateHordeCount(zone))) * (biomeSparsityFactor / 2));
            
            for (int i = 0; i < hordeCount; i++)
            {
                zone.GetLocationOutside(worldRandom, out Vector2 zoneSpawnLocation);
                SpawnHordeAt(zoneSpawnLocation, zone, spawner, hordeCount * 2);
            }

            ulong worldTime = GameManager.Instance.World.worldTime;

            if (!lastSpawned.ContainsKey(zone))
            {
                lastSpawned.Add(zone, worldTime);
            }
            else
            {
                lastSpawned[zone] = worldTime;
            }
        }

        private void SpawnHordeAt(Vector2 location, WorldPOIScanner.POIZone zone, WorldHordeSpawner spawner, int hordeCount)
        {
            float densitySizeRatio = 1.0f;

            if (this.IsDensityInfluencedByZoneProperties())
                 densitySizeRatio = Mathf.Clamp(Mathf.Max(1.0f, zone.GetBounds().size.magnitude / (zone.GetCount() * zone.GetCount() * hordeCount * hordeCount)), 0.0f, 1.0f);
    
            spawner.Spawn<Horde, LocationHordeSpawn>(new LocationHordeSpawn(location), new HordeSpawnParams(20), densitySizeRatio, CreateHordeAICommandGenerator(zone), CreateEntityAICommandGenerator());
        }

        public abstract IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(WorldPOIScanner.POIZone zone);
        public abstract IAICommandGenerator<EntityAICommand> CreateEntityAICommandGenerator();

        public override IData Load(IDataLoader loader)
        {
            Dictionary<WorldPOIScanner.POIZone, ulong> lastSpawnedDictionary = loader.Load<Dictionary<WorldPOIScanner.POIZone, ulong>>();

            foreach (var lastSpawnedEntry in lastSpawnedDictionary)
            {
                this.lastSpawned.Add(lastSpawnedEntry.Key, lastSpawnedEntry.Value);
            }

            return this;
        }

        public override void Save(IDataSaver saver)
        {
            saver.Save<Dictionary<WorldPOIScanner.POIZone, ulong>>(this.lastSpawned);
        }

        public override void Flush()
        {
            this.lastSpawned.Clear();
        }
    }
}
