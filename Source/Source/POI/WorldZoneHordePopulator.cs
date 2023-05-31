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

        private void SpawnHordesAt(WorldPOIScanner.Zone zone, WorldHordeSpawner spawner)
        {
            Vector3 zoneCenter = zone.GetBounds().center;
            int maxRadius = Mathf.RoundToInt(zone.GetBounds().size.magnitude);

            Log.Out("Max radius: " + maxRadius + " D : " + zone.GetDensity());

            int hordeCount = Mathf.CeilToInt(((float)maxRadius / zone.GetCount()) * zone.GetDensity());
            Log.Out("Spawning " + hordeCount + " hordes");

            for (int i = 0; i < hordeCount; i++)
            {
                Vector2 zoneSpawnLocation = new Vector2(zoneCenter.x, zoneCenter.z) + GameManager.Instance.World.GetGameRandom().RandomInsideUnitCircle * maxRadius;
                SpawnHordeAt(zoneSpawnLocation, zone, spawner, hordeCount);
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
