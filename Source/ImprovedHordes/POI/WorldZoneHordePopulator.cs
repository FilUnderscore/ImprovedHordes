using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Populator;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static ImprovedHordes.Core.World.Horde.WorldHordeTracker;

namespace ImprovedHordes.POI
{
    public abstract class WorldZoneHordePopulator<Horde> : HordePopulator<WorldPOIScanner.POIZone> where Horde: IHorde
    {
        private static readonly Setting<ulong> ZONE_HORDE_REPOPULATION_DAYS = new Setting<ulong>("zone_horde_repopulation_days", 7);
        
        private readonly Dictionary<WorldPOIScanner.POIZone, ulong> lastSpawned = new Dictionary<WorldPOIScanner.POIZone, ulong>();

        protected readonly WorldPOIScanner scanner;
        
        private int MAX_VIEW_DISTANCE_SQUARED
        {
            get
            {
                return WorldHordeTracker.MAX_VIEW_DISTANCE * WorldHordeTracker.MAX_VIEW_DISTANCE;
            }
        }

        public WorldZoneHordePopulator(WorldPOIScanner scanner)
        {
            this.scanner = scanner;
        }

        public override bool CanRun(List<PlayerSnapshot> players, Dictionary<Type, List<ClusterSnapshot>> clusters)
        {
            return this.scanner.HasScanCompleted();
        }

        public override void Populate(WorldPOIScanner.POIZone zone, WorldHordeSpawner spawner, GameRandom random)
        {
            if (zone != null)
            {
                SpawnHordesAt(zone, spawner, random);
            }
        }

        protected WorldPOIScanner.POIZone GetRandomZone(GameRandom random)
        {
            var zones = this.scanner.GetZones();
            return zones[random.RandomRange(zones.Count)];
        }

        public override bool CanPopulate(float dt, out WorldPOIScanner.POIZone zone, List<PlayerSnapshot> players, Dictionary<Type, List<ClusterSnapshot>> clusters, GameRandom random)
        {
            WorldPOIScanner.POIZone randomZone = this.GetRandomZone(random);

            if(randomZone.GetDensity() < this.GetMinimumDensity())
            {
                zone = null;
                return false;
            }    

            if (lastSpawned.TryGetValue(randomZone, out ulong spawnTime))
            {
                ulong worldTime = GameManager.Instance.World.worldTime;

                if (worldTime - spawnTime < (24000 * ZONE_HORDE_REPOPULATION_DAYS.Value))
                {
                    zone = null;
                    return false;
                }
            }

            bool nearby = false;

            // Check for nearby players.
            Parallel.ForEach(players, player =>
            {
                if ((randomZone.GetBounds().center - player.location).sqrMagnitude <= MAX_VIEW_DISTANCE_SQUARED)
                {
                    nearby |= true;
                }
            });

            if (!nearby)
            {
                // Check for nearby hordes.
                Parallel.ForEach(clusters[typeof(Horde)], cluster =>
                {
                    if ((randomZone.GetBounds().center - cluster.location).sqrMagnitude <= MAX_VIEW_DISTANCE_SQUARED)
                    {
                        nearby |= true;
                    }
                });
            }

            zone = randomZone;
            return !nearby;
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

        private void SpawnHordesAt(WorldPOIScanner.POIZone zone, WorldHordeSpawner spawner, GameRandom random)
        {
            Vector3 zoneCenter = zone.GetBounds().center;
            int maxRadius = Mathf.RoundToInt(zone.GetBounds().size.magnitude) / 4;

            float biomeFactor = HordeBiomes.DetermineBiomeFactor(zoneCenter);
            int hordeCount = Mathf.CeilToInt(Mathf.Max(1, Mathf.FloorToInt(CalculateHordeCount(zone))) * (biomeFactor / 2));
            
            for (int i = 0; i < hordeCount; i++)
            {
                Vector2 zoneSpawnLocation = new Vector2(zoneCenter.x, zoneCenter.z) + random.RandomInsideUnitCircle * maxRadius;
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
            float maxBiomeDensity = HordeBiomes.DetermineBiomeDensity(location);
            float densitySizeRatio = 1.0f;

            if (this.IsDensityInfluencedByZoneProperties())
                 densitySizeRatio = Mathf.Min(maxBiomeDensity, Mathf.Max(1.0f, zone.GetBounds().size.magnitude / (zone.GetCount() * zone.GetCount() * hordeCount * hordeCount)));
    
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
