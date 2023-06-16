using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.Threading;
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
        private const ulong RESPAWN_DELAY = 24000 * 3;
        private readonly Dictionary<WorldPOIScanner.POIZone, ulong> lastSpawned = new Dictionary<WorldPOIScanner.POIZone, ulong>();

        protected readonly WorldPOIScanner scanner;
        
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

                if (worldTime - spawnTime < RESPAWN_DELAY)
                {
                    zone = null;
                    return false;
                }
            }

            bool nearby = false;

            // Check for nearby players.
            Parallel.ForEach(players, player =>
            {
                if ((randomZone.GetBounds().center - player.location).sqrMagnitude <= randomZone.GetBounds().size.sqrMagnitude)
                {
                    nearby |= true;
                }
            });

            if (!nearby)
            {
                // Check for nearby hordes.
                Parallel.ForEach(clusters[typeof(Horde)], cluster =>
                {
                    if ((randomZone.GetBounds().center - cluster.location).sqrMagnitude <= randomZone.GetBounds().size.sqrMagnitude)
                    {
                        nearby |= true;
                    }
                });
            }

            zone = randomZone;
            return !nearby;
        }

        protected abstract int CalculateHordeCount(WorldPOIScanner.POIZone zone);

        protected virtual float GetMinimumDensity()
        {
            return 0.0f;
        }

        private void SpawnHordesAt(WorldPOIScanner.POIZone zone, WorldHordeSpawner spawner, GameRandom random)
        {
            Vector3 zoneCenter = zone.GetBounds().center;
            int maxRadius = Mathf.RoundToInt(zone.GetBounds().size.magnitude);

            int hordeCount = CalculateHordeCount(zone);
            
            for (int i = 0; i < hordeCount; i++)
            {
                Vector2 zoneSpawnLocation = new Vector2(zoneCenter.x, zoneCenter.z) + random.RandomInsideUnitCircle * maxRadius;
                SpawnHordeAt(zoneSpawnLocation, zone, spawner, hordeCount, random);
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

        private void SpawnHordeAt(Vector2 location, WorldPOIScanner.POIZone zone, WorldHordeSpawner spawner, int hordeCount, GameRandom random)
        {
            float zoneSpawnDensity = (zone.GetDensity() * 1.0f);
            spawner.Spawn<Horde, LocationHordeSpawn>(new LocationHordeSpawn(location), new HordeSpawnData(20), zoneSpawnDensity, CreateHordeAICommandGenerator(zone), CreateEntityAICommandGenerator());

            //Log.Out($"Spawned horde of density {zoneSpawnDensity} at {location}.");
        }

        public abstract IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(WorldPOIScanner.POIZone zone);
        public abstract IAICommandGenerator<EntityAICommand> CreateEntityAICommandGenerator();
    }
}
