using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.AI;
using ImprovedHordes.Core.World.Horde.Cluster.Data;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.Core.World.Horde.Spawn.Request;
using System;
using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.Cluster
{
    public sealed class HordeCluster : ISaveable<HordeClusterData>
    {
        public enum SpawnState
        {
            SPAWNED,
            SPAWNING,
            DESPAWNED,
            DESPAWNING
        }

        private readonly IHorde horde;
        private HordeEntityGenerator previousHordeEntityGenerator;

        private float density;
        private float densityPerEntity;
        private HordeClusterSpawnRequest? spawnRequest; // Used to keep track of spawning.

        private readonly IAICommandGenerator<EntityAICommand> entityCommandGenerator;
        private readonly List<HordeClusterEntity> entities = new List<HordeClusterEntity>();
        private SpawnState spawnState = SpawnState.DESPAWNED;

        public HordeCluster(IHorde horde, float density, IAICommandGenerator<EntityAICommand> entityCommandGenerator)
        {
            this.horde = horde;
            this.density = density;
            this.entityCommandGenerator = entityCommandGenerator;
        }

        public HordeCluster(HordeClusterData data)
        {
            this.horde = data.GetHorde();
            this.density = data.GetDensity();
            this.entityCommandGenerator = data.GetEntityCommandGenerator();
        }

        public IHorde GetHorde()
        {
            return this.horde;
        }

        public HordeEntityGenerator GetPreviousHordeEntityGenerator()
        {
            return this.previousHordeEntityGenerator;
        }

        public void SetPreviousHordeEntityGenerator(HordeEntityGenerator hordeEntityGenerator)
        {
            this.previousHordeEntityGenerator = hordeEntityGenerator;
        }

        public void RequestSpawn(WorldHorde horde, HordeSpawnParams spawnParams, WorldHordeSpawner spawner, PlayerHordeGroup group, MainThreadRequestProcessor mainThreadRequestProcessor, IWorldRandom worldRandom, HordeAIExecutor aiExecutor, Action<IEntity> onSpawn)
        {
            if (this.spawnState != SpawnState.DESPAWNED)
            {
                if(this.spawnState == SpawnState.SPAWNING)
                {
                    throw new InvalidOperationException("Cannot request horde cluster spawn when already spawning.");
                }

                return;
            }

            this.spawnRequest = spawner.RequestSpawn(horde, this, group, spawnParams, entity =>
            {
                HordeClusterEntity clusterEntity = new HordeClusterEntity(this, entity, horde.GetCharacteristics());
                this.AddEntity(horde, clusterEntity);

                aiExecutor.AddEntity(clusterEntity, worldRandom, this.entityCommandGenerator, mainThreadRequestProcessor);

                if (onSpawn != null)
                    onSpawn(entity);
            }, () =>
            {
                this.spawnState = SpawnState.SPAWNED;
            });

            this.spawnState = SpawnState.SPAWNING;
        }

        public bool TryGetSpawnRequest(out HordeClusterSpawnRequest spawnRequest)
        {
            if(this.spawnRequest == null)
            {
                spawnRequest = default;
                return false;
            }

            spawnRequest = this.spawnRequest.Value;
            return true;
        }

        public float GetDensity() 
        {
            return this.density;
        }

        public float GetDensityPerEntity() 
        {
            return this.densityPerEntity;
        }

        public void NotifyDensityRemoved()
        {
            this.density -= this.densityPerEntity;
        }

        public void Decay(float dt)
        {
            const float decayRate = 0.001f;
            this.density -= decayRate * dt;
        }

        public bool IsDead()
        {
            return this.density <= float.Epsilon;
        }

        public void AddEntity(WorldHorde worldHorde, HordeClusterEntity entity)
        {
            worldHorde.SetSpawnedHordeEntityCount(worldHorde.GetSpawnedHordeEntityCount() + 1);

            this.entities.Add(entity);
            this.densityPerEntity = this.density / this.entities.Count;
        }

        public void RemoveEntity(WorldHorde worldHorde, HordeClusterEntity entity) 
        {
            worldHorde.SetSpawnedHordeEntityCount(worldHorde.GetSpawnedHordeEntityCount() - 1);

            this.entities.Remove(entity);
        }

        public List<HordeClusterEntity> GetEntities()
        {
            return this.entities;
        }

        public void SetSpawnState(SpawnState spawnState)
        {
            this.spawnState = spawnState;
        }

        public SpawnState GetSpawnState()
        {
            return this.spawnState;
        }

        public int GetEntitiesSpawned()
        {
            int entitiesSpawned = 0;

            foreach(var entity in this.entities)
            {
                if (entity.IsSpawned())
                    entitiesSpawned++;
            }

            return entitiesSpawned;
        }

        public IAICommandGenerator<EntityAICommand> GetEntityCommandGenerator()
        {
            return this.entityCommandGenerator;
        }

        public HordeClusterData GetData()
        {
            return new HordeClusterData(this.horde, this.density, this.entityCommandGenerator);
        }
    }
}
