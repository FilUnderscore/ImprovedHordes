using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.POI;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WorldWanderingHordePopulator
    {
        private readonly WorldPOIScanner worldPOIScanner;
        private readonly WorldHordeTracker hordeTracker;
        private readonly WorldHordeSpawner hordeSpawner;

        private bool initiallyPopulated;
        private double lastPopulationTime;

        private Task<WorldPOIScanner.Zone> PopulateCheckTask;

        public WorldWanderingHordePopulator(WorldPOIScanner worldPOIScanner, WorldHordeTracker hordeTracker, WorldHordeSpawner hordeSpawner)
        { 
            this.worldPOIScanner = worldPOIScanner;
            this.hordeTracker = hordeTracker;
            this.hordeSpawner = hordeSpawner;
        }

        public void Update()
        {
            if (!this.worldPOIScanner.HasScanCompleted())
                return;

            if (!CanPopulate())
                return;

            if(PopulateCheckTask != null && PopulateCheckTask.IsCompleted)
            {
                WorldPOIScanner.Zone zone = PopulateCheckTask.Result;

                if(zone != null)
                {
                    SpawnHordeAt(zone);
                }
            }

            if(PopulateCheckTask == null || PopulateCheckTask.IsCompleted)
            {
                PopulateCheckTask = Task.Run(() =>
                {
                    WorldPOIScanner.Zone randomZone = worldPOIScanner.PickRandomZone();
                    bool nearby = false;

                    Parallel.ForEach(hordeTracker.GetClustersOf<WanderingHorde>(), cluster =>
                    {
                        if ((randomZone.GetBounds().center - cluster.location).sqrMagnitude <= randomZone.GetBounds().size.sqrMagnitude)
                        {
                            nearby = true;
                        }
                    });

                    return !nearby ? randomZone : null;
                });
            }
        }

        private bool CanPopulate()
        {
            if(!initiallyPopulated)
            {
                if (this.hordeTracker.GetClustersOf<WanderingHorde>().Count >= this.worldPOIScanner.GetZoneCount())
                    initiallyPopulated = true;
            }

            return this.hordeTracker.GetClustersOf<WanderingHorde>().Count < this.worldPOIScanner.GetZoneCount() && 
                   (Time.timeAsDouble - this.lastPopulationTime > 60.0 || !initiallyPopulated);
        }

        private void SpawnHordeAt(WorldPOIScanner.Zone zone)
        {
            Vector3 zoneSpawnLocation = zone.GetBounds().center;
            float zoneSpawnDensity = Mathf.Max(1.0f, zone.GetDensity() * 2.0f);

            this.hordeSpawner.Spawn<WanderingHorde, LocationHordeSpawn>(new LocationHordeSpawn(new Vector2(zoneSpawnLocation.x, zoneSpawnLocation.z)), zoneSpawnDensity);
            this.lastPopulationTime = Time.timeAsDouble;

            Log.Out($"Spawned at {zoneSpawnLocation} Zone spawn density {zoneSpawnDensity}");
        }
    }
}
