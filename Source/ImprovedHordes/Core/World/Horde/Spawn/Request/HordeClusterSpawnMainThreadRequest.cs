using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public sealed class HordeClusterSpawnMainThreadRequest : IMainThreadRequest
    {
        private readonly Abstractions.Logging.ILogger logger;
        private readonly IEntitySpawner spawner;

        private readonly HordeEntityGenerator generator;

        private readonly WorldHorde horde;
        private readonly HordeCluster cluster;
        private readonly PlayerHordeGroup playerGroup;
        private readonly HordeSpawnParams spawnData;

        private readonly int size;
        private int index;

        private readonly Action<IEntity> onSpawnAction;
        private readonly Action onSpawnedAction;

        private readonly IRandomFactory<IWorldRandom> randomFactory;
        private readonly IWorldRandom random;

        private readonly ThreadSubscription<HordeClusterSpawnState> spawnState;

        public HordeClusterSpawnMainThreadRequest(ILoggerFactory loggerFactory, IEntitySpawner spawner, IRandomFactory<IWorldRandom> randomFactory, WorldHorde horde, HordeCluster cluster, PlayerHordeGroup playerGroup, HordeSpawnParams spawnData, Action<IEntity> onSpawnAction, Action onSpawned)
        {
            this.logger = loggerFactory.Create(typeof(HordeClusterSpawnMainThreadRequest));
            this.spawner = spawner;

            this.horde = horde;
            this.cluster = cluster;
            this.playerGroup = playerGroup;
            this.spawnData = spawnData;

            this.randomFactory = randomFactory;
            this.random = randomFactory.CreateRandom(this.cluster.GetHashCode());
            this.generator = DetermineEntityGenerator(playerGroup, this.random);

            this.size = this.generator.DetermineEntityCount(cluster.GetDensity());
            this.index = 0;

            this.onSpawnAction = onSpawnAction;
            this.onSpawnedAction = onSpawned;

            this.spawnState = new ThreadSubscription<HordeClusterSpawnState>();
        }

        public HordeClusterSpawnRequest GetSpawnRequest()
        {
            return new HordeClusterSpawnRequest(this.horde, this.cluster, this.playerGroup, this.spawnData, this.spawnState.Subscribe());
        }

        private HordeEntityGenerator DetermineEntityGenerator(PlayerHordeGroup playerGroup, IRandom random)
        {
            HordeEntityGenerator generator = this.cluster.GetPreviousHordeEntityGenerator();

            if(generator == null || !generator.IsStillValidFor(playerGroup)) 
            {
                return this.cluster.GetHorde().CreateEntityGenerator(playerGroup, random);
            }
            else
            {
                generator.SetPlayerGroup(playerGroup);
                return generator;
            }
        }

        private int GetWorldEntitiesAlive()
        {
            switch(this.cluster.GetHorde().GetHordeType())
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
            switch(this.cluster.GetHorde().GetHordeType())
            {
                case HordeType.ANIMAL:
                    return GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedAnimals);
                case HordeType.ENEMY:
                    return GamePrefs.GetInt(EnumGamePrefs.MaxSpawnedZombies);
                default:
                    return 0;
            }
        }

        public bool IsDone()
        {
            return this.index >= this.size;
        }

        public void OnCleanup()
        {
            this.randomFactory.FreeRandom(this.random);

            if (this.onSpawnedAction != null)
                this.onSpawnedAction.Invoke();
        }

        private const float SPAWN_DELAY = 10.0f;
        private float spawnTicks;

        public void TickExecute(float dt)
        {
            if (--spawnTicks > 0.0f) // Slight spawn delay prevents immediate stuttering when spawning large groups of enemies.
                return;
            else
                spawnTicks = SPAWN_DELAY;

            if (GetWorldEntitiesAlive() >= GetMaxAllowedWorldEntitiesAlive()) // World is currently overpopulated, so skip this update.
                return;

            int MAX_ENTITIES_SPAWNED_PER_PLAYER = WorldHordeTracker.MAX_ENTITIES_SPAWNED_PER_PLAYER.Value;

            if (MAX_ENTITIES_SPAWNED_PER_PLAYER > -1 && this.horde.GetSpawnedHordeEntityCount() >= MAX_ENTITIES_SPAWNED_PER_PLAYER * this.playerGroup.GetCount()) // Cannot exceed the max number of entities per player for performance reasons.
                return;

            Vector3 spawnTargetLocation = this.horde.GetLocation();
            bool playersNearby;

            do
            {
                playersNearby = false;

                Vector3 closestLocation;
                Vector3 closestDirection;

                foreach (var player in this.playerGroup.GetPlayers())
                {
                    Vector3 direction = (this.horde.GetLocation() - player.location).normalized;
                    float distance = WorldHordeTracker.SPAWN_VIEW_DISTANCE;
                    float playerDistance = Vector3.Distance(spawnTargetLocation, player.location);

                    if (playerDistance <= distance / 2)
                    {
                        closestDirection = direction;
                        closestLocation = player.location;

                        playersNearby = true;
                        spawnTargetLocation += direction * distance; // <= distance will cause this loop to hang because direction does not increment location.
                        break;
                    }
                }
            } while (playersNearby);

            if (!GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(spawnTargetLocation, 0, this.spawnData.SpreadDistanceLimit, -1, true, out Vector3 spawnLocation, false))
            {
                this.logger.Warn($"Bad spawn request for horde at {spawnTargetLocation}");

                this.spawnState.Update(new HordeClusterSpawnState(this.index, this.size - this.index, true));

                // Cancel spawn if player is too far.
                this.index = this.size;

                return;
            }

            this.onSpawnAction.Invoke(spawner.SpawnAt(this.generator.GetEntityClassId(this.random), spawnLocation));
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
    }
}
