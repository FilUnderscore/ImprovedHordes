using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public sealed class HordeEntitySpawnRequest : IMainThreadRequest
    {
        private readonly Abstractions.Logging.ILogger logger;
        private readonly IEntitySpawner spawner;
        private readonly WorldHorde horde;
        private readonly PlayerHordeGroup playerGroup;
        private readonly HordeClusterEntity entity;
        private readonly Action<IEntity> onSpawn;

        private bool done = false;

        public HordeEntitySpawnRequest(ILoggerFactory loggerFactory, IEntitySpawner spawner, WorldHorde horde, PlayerHordeGroup playerGroup, HordeClusterEntity entity, Action<IEntity> onSpawn)
        {
            this.logger = loggerFactory.Create(typeof(HordeEntitySpawnRequest));
            this.spawner = spawner;
            this.horde = horde;
            this.playerGroup = playerGroup;
            this.entity = entity;
            this.onSpawn = onSpawn;
        }

        public bool IsDone()
        {
            return done || (!this.entity.IsAwaitingSpawnStateChange() && this.entity.IsSpawned()) || !this.entity.GetCluster().Spawned;
        }

        public void OnCleanup()
        {
        }

        private int GetWorldEntitiesAlive()
        {
            switch (this.entity.GetCluster().GetHorde().GetHordeType())
            {
                case HordeType.ANIMAL:
                    return GameStats.GetInt(EnumGameStats.AnimalCount);
                case HordeType.ENEMY:
                    return GameStats.GetInt(EnumGameStats.EnemyCount);
                default:
                    return 0;
            }
        }

        private int GetMaxAllowedWorldEntitiesAlive()
        {
            switch (this.entity.GetCluster().GetHorde().GetHordeType())
            {
                case HordeType.ANIMAL:
                    return GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedAnimals);
                case HordeType.ENEMY:
                    return GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedZombies);
                default:
                    return 0;
            }
        }

        public void TickExecute(float dt)
        {
            if (GetWorldEntitiesAlive() >= GetMaxAllowedWorldEntitiesAlive() * WorldHordeTracker.MAX_SPAWN_CAPACITY_PERCENT.Value) // World is currently overpopulated, so skip this update.
                return;

            int MAX_ENTITIES_SPAWNED_PER_PLAYER = WorldHordeTracker.MAX_ENTITIES_SPAWNED_PER_PLAYER.Value;

            if (MAX_ENTITIES_SPAWNED_PER_PLAYER > -1 && this.horde.GetSpawnedHordeEntityCount() >= MAX_ENTITIES_SPAWNED_PER_PLAYER * this.playerGroup.GetCount()) // Cannot exceed the max number of entities per player for performance reasons.
            {
                return;
            }

            if(!TryCalculateNewDirectionalHordeEntitySpawnPosition(this.entity.GetLocation(), out Vector3 spawnLocation))
            {
                // Retry spawns until players are far enough.
                this.playerGroup.GetPlayerClosestTo(spawnLocation, out float distance);

                if (distance > WorldHordeTracker.MAX_UNLOAD_VIEW_DISTANCE)
                {
#if DEBUG
                    this.logger.Warn("Failed to respawn horde entity.");
#endif
                    this.done = true;
                }

#if DEBUG
                this.logger.Warn("Could not calculate horde entity spawn.");
#endif

                return;
            }

            if (this.entity.Respawn(this.logger, this.spawner, spawnLocation))
            {
                if (this.onSpawn != null)
                    this.onSpawn(this.entity.GetEntity());
            }
            else
            {
#if DEBUG
                this.logger.Warn($"Bad entity spawn at {spawnLocation}");
#endif

                done = true; // Skip the spawn request since it's a bad entity spawn.
            }
        }

        private bool TryCalculateNewDirectionalHordeEntitySpawnPosition(Vector3 previousSpawnPosition, out Vector3 spawnPosition)
        {
            int minSpawnDistance = WorldHordeTracker.MIN_SPAWN_VIEW_DISTANCE;
            int maxSpawnDistance = WorldHordeTracker.MAX_SPAWN_VIEW_DISTANCE;
            int targetSpawnDistance = (minSpawnDistance + maxSpawnDistance) / 2;

            do
            {
                PlayerSnapshot closestPlayer = this.playerGroup.GetPlayerClosestTo(previousSpawnPosition, out float playerDistance);
                Vector3 direction = (previousSpawnPosition - closestPlayer.location).normalized;

                if (playerDistance >= minSpawnDistance - 1 && playerDistance <= maxSpawnDistance + 1) // Be slightly lenient because floats don't play well with ints in this setting.
                    break;

                previousSpawnPosition += direction * (targetSpawnDistance - playerDistance); // Careful, this can cause the loop to hang if not properly checked.
            } while (true);

            spawnPosition = previousSpawnPosition;
            return true;
        }
    }
}
