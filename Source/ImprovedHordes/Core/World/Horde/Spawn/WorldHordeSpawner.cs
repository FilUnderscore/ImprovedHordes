using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using ImprovedHordes.Core.World.Horde.Spawn.Request;
using System;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Spawn
{
    public sealed class WorldHordeSpawner
    {
        private readonly ILoggerFactory loggerFactory;
        private readonly IRandomFactory<IWorldRandom> randomFactory;
        private readonly IEntitySpawner entitySpawner;
        private readonly WorldHordeTracker hordeTracker;
        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;

        public WorldHordeSpawner(ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory, IEntitySpawner entitySpawner, WorldHordeTracker hordeTracker, MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            this.loggerFactory = loggerFactory;
            this.randomFactory = randomFactory;
            this.entitySpawner = entitySpawner;
            this.hordeTracker = hordeTracker;
            this.mainThreadRequestProcessor = mainThreadRequestProcessor;

            this.hordeTracker.SetHordeSpawner(this);
        }

        public void Spawn<Horde, HordeSpawn>(HordeSpawn spawn, HordeSpawnParams spawnParams, IAICommandGenerator<AICommand> commandGenerator, IAICommandGenerator<EntityAICommand> entityCommandGenerator) where Horde : IHorde where HordeSpawn : IHordeSpawn
        {
            this.Spawn<Horde, HordeSpawn>(spawn, spawnParams, 1.0f, commandGenerator, entityCommandGenerator);
        }

        public void Spawn<Horde, HordeSpawn>(HordeSpawn spawn, HordeSpawnParams spawnParams, float density, IAICommandGenerator<AICommand> commandGenerator, IAICommandGenerator<EntityAICommand> entityCommandGenerator) where Horde : IHorde where HordeSpawn : IHordeSpawn
        {
            Horde horde = Activator.CreateInstance<Horde>();

            Vector2 surfaceSpawnLocation = spawn.DetermineSurfaceLocation();
            float surfaceSpawnHeight = GameManager.Instance.World.GetHeightAt(surfaceSpawnLocation.x, surfaceSpawnLocation.y) + 1.0f;

            Vector3 spawnLocation = new Vector3(surfaceSpawnLocation.x, surfaceSpawnHeight, surfaceSpawnLocation.y);
            BiomeDefinition spawnBiome = HordeBiomes.GetBiomeAt(spawnLocation);

            this.hordeTracker.Add(new WorldHorde(spawnLocation, new HordeSpawnData(spawnParams, spawnBiome), horde, density, this.randomFactory, commandGenerator, entityCommandGenerator));
        }

        public HordeClusterSpawnRequest RequestSpawn(WorldHorde horde, HordeCluster cluster, PlayerHordeGroup playerGroup, HordeSpawnParams spawnParams, Action<IEntity> onEntitySpawn, Action onSpawned)
        {
            HordeClusterSpawnMainThreadRequest mainThreadRequest = new HordeClusterSpawnMainThreadRequest(this.loggerFactory, this.entitySpawner, this.randomFactory, horde, cluster, playerGroup, spawnParams, onEntitySpawn, onSpawned);
            this.mainThreadRequestProcessor.Request(mainThreadRequest);

            return mainThreadRequest.GetSpawnRequest();
        }
    }
}