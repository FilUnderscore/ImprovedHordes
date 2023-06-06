using ImprovedHordes.Source.Core.Horde;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;
using ImprovedHordes.Source.Core.Horde.World.Populator;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.POI
{
    public abstract class WorldZoneHordePopulator<Horde> : HordePopulator<WorldPOIScanner.Zone> where Horde: IHorde
    {
        private const ulong RESPAWN_DELAY = 30000;
        private readonly Dictionary<WorldPOIScanner.Zone, ulong> lastSpawned = new Dictionary<WorldPOIScanner.Zone, ulong>();

        protected readonly WorldPOIScanner scanner;
        
        public WorldZoneHordePopulator(WorldPOIScanner scanner)
        {
            this.scanner = scanner;
        }

        public override bool CanRun(WorldHordeTracker tracker)
        {
            return this.scanner.HasScanCompleted();
        }

        public override void Populate(WorldPOIScanner.Zone zone, WorldHordeSpawner spawner, GameRandom random)
        {
            if (zone != null)
            {
                SpawnHordesAt(zone, spawner, random);
            }
        }

        protected WorldPOIScanner.Zone GetRandomZone(GameRandom random)
        {
            var zones = this.scanner.GetZones();
            return zones[random.RandomRange(zones.Count)];
        }

        public override bool CanPopulate(float dt, out WorldPOIScanner.Zone zone, WorldHordeTracker tracker, GameRandom random)
        {
            WorldPOIScanner.Zone randomZone = this.GetRandomZone(random);

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
            Parallel.ForEach(tracker.GetPlayers(), player =>
            {
                if ((randomZone.GetBounds().center - player.location).sqrMagnitude <= randomZone.GetBounds().size.sqrMagnitude)
                {
                    nearby |= true;
                }
            });

            if (!nearby)
            {
                // Check for nearby hordes.
                Parallel.ForEach(tracker.GetClustersOf<Horde>(), cluster =>
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

        protected abstract int CalculateHordeCount(WorldPOIScanner.Zone zone);

        private void SpawnHordesAt(WorldPOIScanner.Zone zone, WorldHordeSpawner spawner, GameRandom random)
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

        private void SpawnHordeAt(Vector2 location, WorldPOIScanner.Zone zone, WorldHordeSpawner spawner, int hordeCount, GameRandom random)
        {
            float zoneSpawnDensity = (zone.GetDensity() * 1.0f) / hordeCount;
            spawner.Spawn<Horde, LocationHordeSpawn>(new LocationHordeSpawn(location), new HordeSpawnData(20), zoneSpawnDensity, CreateHordeAICommandGenerator(zone));

            //Log.Out($"Spawned horde of density {zoneSpawnDensity} at {location}.");
        }

        public abstract IAICommandGenerator CreateHordeAICommandGenerator(WorldPOIScanner.Zone zone);
    }
}
