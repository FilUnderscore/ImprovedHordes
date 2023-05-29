using ImprovedHordes.Source.Core.Horde;
using ImprovedHordes.Source.Core.Horde.World;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.POI
{
    public abstract class WorldZoneHordePopulator<Horde> : WorldHordePopulator<WorldPOIScanner.Zone> where Horde: IHorde
    {
        private readonly WorldPOIScanner scanner;
        
        public WorldZoneHordePopulator(WorldHordeTracker tracker, WorldHordeSpawner spawner, WorldPOIScanner scanner) : base(tracker, spawner)
        {
            this.scanner = scanner;
        }

        public override bool CanRun()
        {
            return this.scanner.HasScanCompleted() && this.GetHordesAlive() < this.scanner.GetZoneCount();
        }

        public override void BeforeTaskRestart()
        {
        }

        public override void OnTaskFinish(WorldPOIScanner.Zone zone)
        {
            if (zone != null)
            {
                SpawnHordeAt(zone);
            }
        }

        public override async Task<WorldPOIScanner.Zone> UpdateAsync(float dt)
        {
            return await UpdatePopulateCheckAsync();
        }

        public async Task<WorldPOIScanner.Zone> UpdatePopulateCheckAsync()
        {
            return await Task.Run(() =>
            {
                WorldPOIScanner.Zone randomZone = this.scanner.PickRandomZone();
                bool nearby = false;

                // Check for nearby players.
                Parallel.ForEach(this.tracker.GetPlayers(), player =>
                {
                    if ((randomZone.GetBounds().center - player.location).sqrMagnitude <= randomZone.GetBounds().size.magnitude)
                    {
                        nearby |= true;
                    }
                });

                if (!nearby)
                {
                    // Check for nearby hordes.
                    Parallel.ForEach(this.tracker.GetClustersOf<Horde>(), cluster =>
                    {
                        if ((randomZone.GetBounds().center - cluster.location).sqrMagnitude <= randomZone.GetBounds().size.sqrMagnitude)
                        {
                            nearby |= true;
                        }
                    });
                }

                return !nearby ? randomZone : null;
            });
        }

        protected WorldPOIScanner.Zone GetRandomZone()
        {
            return this.scanner.PickRandomZone();
        }

        private int GetHordesAlive()
        {
            return this.tracker.GetClustersOf<Horde>().Count;
        }

        private void SpawnHordeAt(WorldPOIScanner.Zone zone)
        {
            Vector3 zoneCenter = zone.GetBounds().center;
            int maxRadius = Mathf.RoundToInt(zone.GetBounds().size.magnitude);

            Log.Out("Max radius: " + maxRadius);

            Vector2 zoneSpawnLocation = new Vector2(zoneCenter.x, zoneCenter.z) + GameManager.Instance.World.GetGameRandom().RandomInsideUnitCircle * maxRadius;

            float zoneSpawnDensity = zone.GetDensity() * 4.0f;
            this.spawner.Spawn<Horde, LocationHordeSpawn>(new LocationHordeSpawn(new Vector2(zoneSpawnLocation.x, zoneSpawnLocation.y)), zoneSpawnDensity, CreateHordeCommands(zone).ToArray());

            Log.Out($"Spawned horde of density {zoneSpawnDensity} at {zoneSpawnLocation}.");
        }

        public abstract IEnumerable<AICommand> CreateHordeCommands(WorldPOIScanner.Zone zone);
    }
}
