using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Populator;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ImprovedHordes.Core.World.Horde.WorldHordeTracker;

namespace ImprovedHordes.POI
{
    public class WorldWildernessHordePopulator<Horde> : HordePopulator<Vector2> where Horde : IHorde
    {
        private static readonly Setting<ulong> WILDERNESS_HORDE_REPOPULATION_DAYS = new Setting<ulong>("wilderness_horde_repopulation_days", 7);
        
        private readonly float worldSize;
        protected readonly WorldPOIScanner scanner;

        private readonly HordeSpawnParams spawnData;
        private readonly int sparsityFactor;
        private readonly bool biomeAffectsSparsity;

        private readonly Dictionary<Vector2i, ulong> lastSpawned = new Dictionary<Vector2i, ulong>();

        private int MAX_VIEW_DISTANCE_SQUARED
        {
            get
            {
                return WorldHordeTracker.MAX_UNLOAD_VIEW_DISTANCE * WorldHordeTracker.MAX_UNLOAD_VIEW_DISTANCE;
            }
        }

        public WorldWildernessHordePopulator(float worldSize, WorldPOIScanner scanner, HordeSpawnParams spawnData, int sparsityFactor, bool biomeAffectsSparsity)
        {
            this.worldSize = worldSize;
            this.scanner = scanner;

            this.spawnData = spawnData;
            this.sparsityFactor = sparsityFactor;
            this.biomeAffectsSparsity = biomeAffectsSparsity;
        }

        private Vector2i GetRegionFromPosition(Vector2 pos)
        {
            float biomeSparsityFactor = this.biomeAffectsSparsity ? HordeBiomes.DetermineBiomeSparsityFactor(pos) : 1.0f;

            int regionX = Mathf.FloorToInt((pos.x * biomeSparsityFactor) / (this.sparsityFactor * MAX_UNLOAD_VIEW_DISTANCE / 4));
            int regionY = Mathf.FloorToInt((pos.y * biomeSparsityFactor) / (this.sparsityFactor * MAX_UNLOAD_VIEW_DISTANCE / 4));

            return new Vector2i(regionX, regionY);
        }

        public override bool CanPopulate(float dt, out Vector2 pos, List<PlayerHordeGroup> playerGroups, Dictionary<Type, List<ClusterSnapshot>> clusters, IWorldRandom worldRandom)
        {
            Vector2 randomWorldPos = worldRandom.RandomLocation2;

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

            foreach(var zone in this.scanner.GetAllZones())
            {
                Vector3 zoneWorldPos = new Vector3(randomWorldPos.x, zone.GetBounds().center.y, randomWorldPos.y);

                if (zone.GetBounds().Contains(zoneWorldPos))
                {
                    pos = Vector3.zero;
                    return false;
                }
            }

            // Check for nearby players.

            foreach(var playerGroup in playerGroups)
            {
                playerGroup.GetPlayerClosestTo(randomWorldPos, out float distance);

                if (distance * distance <= MAX_VIEW_DISTANCE_SQUARED)
                {
                    pos = Vector3.zero;
                    return false;
                }
            }

            // Check for nearby hordes.
            foreach (var cluster in clusters[typeof(Horde)])
            {
                Vector2 clusterPos = new Vector2(cluster.location.x, cluster.location.z);

                if ((randomWorldPos - clusterPos).sqrMagnitude <= MAX_VIEW_DISTANCE_SQUARED)
                {
                    pos = Vector3.zero;
                    return false;
                }
            }

            pos = randomWorldPos;
            return true;
        }

        public override void Populate(Vector2 pos, WorldHordeSpawner spawner, IWorldRandom worldRandom)
        {
            float density = worldRandom.RandomFloat;
            BiomeDefinition biome = HordeBiomes.GetBiomeAt(pos, true);

            spawner.Spawn<Horde, LocationHordeSpawn>(new LocationHordeSpawn(pos), this.spawnData, density, CreateHordeAICommandGenerator(biome), CreateEntityAICommandGenerator(biome));

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

        public virtual IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(BiomeDefinition biome)
        {
            return null;
        }

        public virtual IAICommandGenerator<EntityAICommand> CreateEntityAICommandGenerator(BiomeDefinition biome)
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

        public override void Flush()
        {
            this.lastSpawned.Clear();
        }
    }
}
