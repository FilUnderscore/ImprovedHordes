using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public sealed class HordeClusterEntityGenerateSpawnRequest : IMainThreadRequest
    {
        private readonly Abstractions.Logging.ILogger logger;

        private readonly HordeEntityGenerator generator;

        private readonly WorldHorde horde;

        private readonly HordeCluster cluster;
        private readonly PlayerHordeGroup playerGroup;
        private readonly HordeSpawnParams spawnData;

        private readonly int size;
        private int index;

        private readonly Action onSpawnedAction;

        private readonly IRandomFactory<IWorldRandom> randomFactory;
        private readonly IWorldRandom random;

        private readonly ThreadSubscription<HordeClusterSpawnState> spawnState;

        public HordeClusterEntityGenerateSpawnRequest(ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory, WorldHorde horde, HordeCluster cluster, PlayerHordeGroup playerGroup, HordeSpawnParams spawnData, Action onSpawned)
        {
            this.logger = loggerFactory.Create(typeof(HordeClusterEntityGenerateSpawnRequest));
            
            this.horde = horde;

            this.cluster = cluster;
            this.playerGroup = playerGroup;
            this.spawnData = spawnData;

            this.randomFactory = randomFactory;
            this.random = randomFactory.CreateRandom(this.cluster.GetHashCode());
            this.generator = DetermineEntityGenerator(playerGroup, this.random);

            this.size = this.generator.DetermineEntityCount(cluster.GetDensity());
            this.index = 0;

            this.onSpawnedAction = onSpawned;

            this.spawnState = new ThreadSubscription<HordeClusterSpawnState>();
        }

        public HordeClusterSpawnRequest GetSpawnRequest()
        {
            return new HordeClusterSpawnRequest(this.horde, this.cluster, this.playerGroup, this.spawnData, this.spawnState.Subscribe());
        }

        private HordeEntityGenerator DetermineEntityGenerator(PlayerHordeGroup playerGroup, IRandom random)
        {
            // TODO: this.cluster.GetPreviousHordeEntityGenerator();
            return this.cluster.GetHorde().CreateEntityGenerator(playerGroup, random);
        }

        public bool IsDone()
        {
            return this.index >= this.size;
        }

        public void OnCleanup()
        {
            this.randomFactory.FreeRandom(this.random);
            this.onSpawnedAction?.Invoke();
        }

        private const float SPAWN_DELAY = 2.0f;
        private float spawnTicks;

        public void TickExecute(float dt)
        {
            if (!this.cluster.Spawning) // If the cluster state changes from spawning, then cancel further spawns.
            {
#if DEBUG
                this.logger.Warn("Cluster stopped spawning.");
#endif

                this.Cancel();
                return;
            }

            if (--spawnTicks > 0.0f) // Slight spawn delay prevents immediate stuttering when spawning large groups of enemies.
                return;
            else
                spawnTicks = SPAWN_DELAY;

            if(!TryCalculateHordeEntitySpawnPosition(out Vector3 spawnLocation))
            {
                // Retry spawns until players are far enough.
                this.playerGroup.GetPlayerClosestTo(spawnLocation, out float distance);

                if(distance > WorldHordeTracker.MAX_UNLOAD_VIEW_DISTANCE)
                {
#if DEBUG
                    this.logger.Warn("Failed to spawn horde");
#endif

                    this.Cancel();
                }

#if DEBUG
                this.logger.Warn("Could not calculate spawns");
#endif

                return;
            }

            HordeClusterEntity entity = new HordeClusterEntity(cluster, this.generator.GetEntityClassId(this.random), spawnLocation, this.horde.GetCharacteristics());
            this.cluster.AddEntity(entity);
            
            this.index++;
            this.spawnState.Update(new HordeClusterSpawnState(this.index, this.size - this.index, this.index >= this.size));
        }

        public int GetCount()
        {
            return this.size;
        }

        public int GetRemaining()
        {
            return this.size - this.index;
        }

        public void Cancel()
        {
            this.spawnState.Update(new HordeClusterSpawnState(this.index, this.size - this.index, true));
            this.index = this.size;
        }

        private bool TryCalculateHordeEntitySpawnPosition(out Vector3 spawnLocation)
        {
            // Calculate direction for extra entities.
            return TryCalculateDirectionalHordeEntitySpawnPosition(out spawnLocation);
        }

        private bool TryCalculateDirectionalHordeEntitySpawnPosition(out Vector3 spawnLocation)
        {
            int minSpawnDistance = WorldHordeTracker.MIN_SPAWN_VIEW_DISTANCE;
            int maxSpawnDistance = WorldHordeTracker.MAX_SPAWN_VIEW_DISTANCE;
            int targetSpawnDistance = (minSpawnDistance + maxSpawnDistance) / 2;

            Vector3 spawnTargetLocation = this.horde.GetLocation();

            do
            {
                PlayerSnapshot closestPlayer = this.playerGroup.GetPlayerClosestTo(spawnTargetLocation, out float playerDistance);
                Vector3 direction = (spawnTargetLocation - closestPlayer.location).normalized;

                if (playerDistance >= minSpawnDistance - 1 && playerDistance <= maxSpawnDistance + 1) // Be slightly lenient because floats don't play well with ints in this setting.
                    break;

                spawnTargetLocation += direction * (targetSpawnDistance - playerDistance); // Careful, this can cause the loop to hang if not properly checked.

#if DEBUG
                Log.Out("Dir " + direction + " " + playerDistance);
#endif
            } while (true);

            spawnLocation = spawnTargetLocation;
            return true;
        }
    }
}
