using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.POI;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WorldWanderingHordePopulator
    {
        private const int MAX_ALIVE_WILD = 150;

        private readonly WorldPOIScanner worldPOIScanner;
        private readonly WorldHordeTracker hordeTracker;
        private readonly WorldHordeSpawner hordeSpawner;

        private bool initiallyPopulated;

        private double lastPopulationTimePOI;
        private double lastPopulationTimeRandom;

        private Task<WorldPOIScanner.Zone> PopulateCheckTask;

        public WorldWanderingHordePopulator(WorldPOIScanner worldPOIScanner, WorldHordeTracker hordeTracker, WorldHordeSpawner hordeSpawner)
        { 
            this.worldPOIScanner = worldPOIScanner;
            this.hordeTracker = hordeTracker;
            this.hordeSpawner = hordeSpawner;
        }

        public void Update()
        {
            this.UpdateWild();
            this.UpdatePOI();
        }

        private void UpdateWild() 
        {
            if (!CanPopulateWild())
                return;

            this.SpawnHordeRandom();
        }

        private void UpdatePOI()
        {
            if (!this.worldPOIScanner.HasScanCompleted())
                return;

            if (!CanPopulatePOI())
                return;

            if (PopulateCheckTask != null && PopulateCheckTask.IsCompleted)
            {
                WorldPOIScanner.Zone zone = PopulateCheckTask.Result;

                if (zone != null)
                {
                    SpawnHordeAt(zone);
                }
            }

            if (PopulateCheckTask == null || PopulateCheckTask.IsCompleted)
            {
                PopulateCheckTask = Task.Run(() =>
                {
                    WorldPOIScanner.Zone randomZone = worldPOIScanner.PickRandomZoneGTE(0.5f);
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

        private bool CanPopulate(int max)
        {
            if (!initiallyPopulated)
            {
                if (this.hordeTracker.GetClustersOf<WanderingHorde>().Count >= this.worldPOIScanner.GetZoneCountGTE(0.5f) + MAX_ALIVE_WILD)
                    initiallyPopulated = true;
            }

            return this.hordeTracker.GetClustersOf<WanderingHorde>().Count < max;
        }

        private bool CanPopulateWild()
        {
            return CanPopulate(this.worldPOIScanner.GetZoneCountGTE(0.5f) + MAX_ALIVE_WILD) &&
                (Time.timeAsDouble - this.lastPopulationTimeRandom > 60.0 || !initiallyPopulated); 
        }

        private bool CanPopulatePOI()
        {
            return CanPopulate(this.worldPOIScanner.GetZoneCountGTE(0.5f)) &&
                   (Time.timeAsDouble - this.lastPopulationTimePOI > 60.0 || !initiallyPopulated);
        }

        private void SpawnHordeAt(WorldPOIScanner.Zone zone)
        {
            Vector3 zoneSpawnLocation = zone.GetBounds().center;
            float zoneSpawnDensity = Mathf.Max(1.0f, zone.GetDensity() * 2.0f);

            this.hordeSpawner.Spawn<WanderingHorde, LocationHordeSpawn>(new LocationHordeSpawn(new Vector2(zoneSpawnLocation.x, zoneSpawnLocation.z)), zoneSpawnDensity);
            this.lastPopulationTimePOI = Time.timeAsDouble;

            Log.Out($"Spawned at {zoneSpawnLocation} Zone spawn density {zoneSpawnDensity}");
        }

        private void SpawnHordeRandom()
        {
            this.hordeSpawner.Spawn<WanderingHorde, RandomHordeSpawn>(new RandomHordeSpawn(), GameManager.Instance.World.GetGameRandom().RandomRange(1.5f) + 0.5f);
            this.lastPopulationTimeRandom = Time.timeAsDouble;

            Log.Out($"Spawned random");
        }
    }
}
