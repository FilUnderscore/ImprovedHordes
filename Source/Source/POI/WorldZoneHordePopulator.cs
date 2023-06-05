using ImprovedHordes.Source.Core.Horde;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Populator;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
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

        private readonly WorldPOIScanner scanner;
        
        public WorldZoneHordePopulator(WorldPOIScanner scanner)
        {
            this.scanner = scanner;
        }

        public override bool CanRun()
        {
            return this.scanner.HasScanCompleted();
        }

        public override void Populate(WorldPOIScanner.Zone zone, WorldHordeSpawner spawner)
        {
            if (zone != null)
            {
                SpawnHordesAt(zone, spawner);
            }
        }

        public override bool CanPopulate(float dt, out WorldPOIScanner.Zone zone, WorldHordeTracker tracker)
        {
            WorldPOIScanner.Zone randomZone = this.scanner.PickRandomZone();

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

        protected WorldPOIScanner.Zone GetRandomZone()
        {
            return this.scanner.PickRandomZone();
        }

        protected WorldPOIScanner.Zone GetRandomZone(WorldPOIScanner.Zone zone, float distance)
        {
            return this.scanner.GetRandomZoneFrom(zone, distance);
        }

        protected abstract int CalculateHordeCount(WorldPOIScanner.Zone zone);

        private void SpawnHordesAt(WorldPOIScanner.Zone zone, WorldHordeSpawner spawner)
        {
            Vector3 zoneCenter = zone.GetBounds().center;
            int maxRadius = Mathf.RoundToInt(zone.GetBounds().size.magnitude);

            Log.Out("Max radius: " + maxRadius + " D : " + zone.GetDensity());

            int hordeCount = CalculateHordeCount(zone);
            Log.Out("Spawning " + hordeCount + " hordes");

            for (int i = 0; i < hordeCount; i++)
            {
                Vector2 zoneSpawnLocation = new Vector2(zoneCenter.x, zoneCenter.z) + GameManager.Instance.World.GetGameRandom().RandomInsideUnitCircle * maxRadius;
                SpawnHordeAt(zoneSpawnLocation, zone, spawner, hordeCount);
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

        private void SpawnHordeAt(Vector2 location, WorldPOIScanner.Zone zone, WorldHordeSpawner spawner, int hordeCount)
        {
            float zoneSpawnDensity = (zone.GetDensity() * 8.0f) / hordeCount;
            spawner.Spawn<Horde, LocationHordeSpawn>(new LocationHordeSpawn(location), zoneSpawnDensity, CreateHordeCommands(zone).ToArray());

            Log.Out($"Spawned horde of density {zoneSpawnDensity} at {location}.");
        }

        public abstract IEnumerable<AICommand> CreateHordeCommands(WorldPOIScanner.Zone zone);
    }
}
