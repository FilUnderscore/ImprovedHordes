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
    public class WorldWildernessHordePopulator<Horde> : HordePopulator<Vector2> where Horde : IHorde
    {
        private static readonly Setting<ulong> WILDERNESS_HORDE_REPOPULATION_DAYS = new Setting<ulong>("wilderness_horde_repopulation_days", 7);
        
        private readonly float worldSize;
        protected readonly WorldPOIScanner scanner;

        private readonly HordeSpawnData spawnData;
        private readonly int sparsityFactor;

        private readonly Dictionary<Vector2i, ulong> lastSpawned = new Dictionary<Vector2i, ulong>();

        private int MAX_VIEW_DISTANCE_SQUARED
        {
            get
            {
                return WorldHordeTracker.MAX_VIEW_DISTANCE * WorldHordeTracker.MAX_VIEW_DISTANCE;
            }
        }

        public WorldWildernessHordePopulator(float worldSize, WorldPOIScanner scanner, HordeSpawnData spawnData, int sparsityFactor)
        {
            this.worldSize = worldSize;
            this.scanner = scanner;

            this.spawnData = spawnData;
            this.sparsityFactor = sparsityFactor;
        }

        private Vector2i GetRegionFromPosition(Vector2 pos)
        {
            float biomeFactor = HordeBiomes.DetermineBiomeFactor(pos);

            int regionX = Mathf.FloorToInt((pos.x * biomeFactor) / (this.sparsityFactor * (MAX_VIEW_DISTANCE / 16)));
            int regionY = Mathf.FloorToInt((pos.y * biomeFactor) / (this.sparsityFactor * (MAX_VIEW_DISTANCE / 16)));

            return new Vector2i(regionX, regionY);
        }

        public override bool CanPopulate(float dt, out Vector2 pos, List<PlayerSnapshot> players, Dictionary<Type, List<ClusterSnapshot>> clusters, GameRandom random)
        {
            float randomX = random.RandomFloat * worldSize - worldSize / 2.0f;
            float randomY = random.RandomFloat * worldSize - worldSize / 2.0f;

            Vector2 randomWorldPos = new Vector2(randomX, randomY);

            // Check if any hordes spawned in this area recently.
            if (lastSpawned.TryGetValue(GetRegionFromPosition(randomWorldPos), out ulong spawnTime))
            {
                ulong worldTime = GameManager.Instance.World.worldTime;

                if (worldTime - spawnTime < (24000 * WILDERNESS_HORDE_REPOPULATION_DAYS.Value))
                {
                    pos = Vector2.zero;
                    return false;
                }
            }

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

            float biomeFactor = HordeBiomes.DetermineBiomeFactor(randomWorldPos);

            // Check for nearby players.

            Parallel.ForEach(players, player =>
            {
                Vector2 playerPos = new Vector2(player.location.x, player.location.z);

                if ((randomWorldPos - playerPos).sqrMagnitude <= MAX_VIEW_DISTANCE_SQUARED * ((sparsityFactor / biomeFactor) / 2))
                {
                    nearby |= true;
                }
            });

            if (!nearby)
            {
                // Check for nearby hordes.
                Parallel.ForEach(clusters[typeof(Horde)], cluster =>
                {
                    Vector2 clusterPos = new Vector2(cluster.location.x, cluster.location.z);

                    if ((randomWorldPos - clusterPos).sqrMagnitude <= MAX_VIEW_DISTANCE_SQUARED * (sparsityFactor / biomeFactor))
                    {
                        nearby |= true;
                    }
                });
            }

            pos = randomWorldPos;
            return !nearby;
        }

        public override void Populate(Vector2 pos, WorldHordeSpawner spawner, GameRandom random)
        {
            float density = random.RandomFloat;
            spawner.Spawn<Horde, LocationHordeSpawn>(new LocationHordeSpawn(pos), this.spawnData, density, CreateHordeAICommandGenerator(), CreateEntityAICommandGenerator());

            // Respawn delay for this region.
            ulong worldTime = GameManager.Instance.World.worldTime;

            Vector2i region = GetRegionFromPosition(pos);

            if (!lastSpawned.ContainsKey(region))
            {
                lastSpawned.Add(region, worldTime);
            }
            else
            {
                lastSpawned[region] = worldTime;
            }
        }

        public virtual IAICommandGenerator<AICommand> CreateHordeAICommandGenerator()
        {
            return null;
        }

        public virtual IAICommandGenerator<EntityAICommand> CreateEntityAICommandGenerator()
        {
            return null;
        }

        public override IData Load(IDataLoader loader)
        {
            Dictionary<Vector2i, ulong> lastSpawnedDictionary = loader.Load<Dictionary<Vector2i, ulong>>();

            foreach(var lastSpawnedEntry in lastSpawnedDictionary)
            {
                this.lastSpawned.Add(lastSpawnedEntry.Key, lastSpawnedEntry.Value);
            }

            return this;
        }

        public override void Save(IDataSaver saver)
        {
            saver.Save<Dictionary<Vector2i, ulong>>(this.lastSpawned);
        }
    }
}
