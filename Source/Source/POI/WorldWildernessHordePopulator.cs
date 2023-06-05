using ImprovedHordes.Source.Core.Horde;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;
using ImprovedHordes.Source.Core.Horde.World.Populator;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.POI
{
    public class WorldWildernessHordePopulator<Horde> : HordePopulator<Vector2> where Horde : IHorde
    {
        private readonly float worldSize;
        protected readonly WorldPOIScanner scanner;

        private readonly HordeSpawnData spawnData;

        private int MAX_VIEW_DISTANCE_SQUARED
        {
            get
            {
                return WorldHordeTracker.MAX_VIEW_DISTANCE * WorldHordeTracker.MAX_VIEW_DISTANCE;
            }
        }

        public WorldWildernessHordePopulator(float worldSize, WorldPOIScanner scanner, HordeSpawnData spawnData)
        {
            this.worldSize = worldSize;
            this.scanner = scanner;

            this.spawnData = spawnData;
        }

        public override bool CanPopulate(float dt, out Vector2 pos, WorldHordeTracker tracker)
        {
            GameRandom random = GameManager.Instance.World.GetGameRandom();

            float randomX = random.RandomFloat * worldSize - worldSize / 2.0f;
            float randomY = random.RandomFloat * worldSize - worldSize / 2.0f;

            Vector2 randomWorldPos = new Vector2(randomX, randomY);

            bool inZone = false;
            Parallel.ForEach(this.scanner.GetZones(), zone =>
            {
                Vector3 zoneWorldPos = new Vector3(randomWorldPos.x, zone.GetBounds().center.y, randomWorldPos.y);

                if (zone.GetBounds().Contains(zoneWorldPos))
                    inZone |= true;
            });

            if(inZone)
            {
                pos = Vector3.zero;
                return false;
            }

            bool nearby = false;

            // Check for nearby players.
            Parallel.ForEach(tracker.GetPlayers(), player =>
            {
                Vector2 playerPos = new Vector2(player.location.x, player.location.z);

                if ((randomWorldPos - playerPos).sqrMagnitude <= MAX_VIEW_DISTANCE_SQUARED)
                {
                    nearby |= true;
                }
            });

            if (!nearby)
            {
                // Check for nearby hordes.
                Parallel.ForEach(tracker.GetClustersOf<Horde>(), cluster =>
                {
                    Vector2 clusterPos = new Vector2(cluster.location.x, cluster.location.z);

                    if ((randomWorldPos - clusterPos).sqrMagnitude <= MAX_VIEW_DISTANCE_SQUARED * 16)
                    {
                        nearby |= true;
                    }
                });
            }

            pos = randomWorldPos;
            return !nearby;
        }

        public override void Populate(Vector2 pos, WorldHordeSpawner spawner)
        {
            float density = GameManager.Instance.World.GetGameRandom().RandomFloat;
            spawner.Spawn<Horde, LocationHordeSpawn>(new LocationHordeSpawn(pos), this.spawnData, density, CreateHordeAICommandGenerator());
        }

        public virtual IAICommandGenerator CreateHordeAICommandGenerator()
        {
            return null;
        }
    }
}
