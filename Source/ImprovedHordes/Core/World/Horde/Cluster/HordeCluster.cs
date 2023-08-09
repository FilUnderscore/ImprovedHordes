using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.Cluster.Data;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.Cluster
{
    public sealed class HordeCluster : ISaveable<HordeClusterData>
    {
        private readonly IHorde horde;
        private HordeClusterDensity density; // Don't make this readonly or hordes will infinitely spawn...
        private readonly IAICommandGenerator<EntityAICommand> entityCommandGenerator;

        private EHordeClusterSpawnState spawnState = EHordeClusterSpawnState.DESPAWNED;

        private readonly List<HordeClusterEntity> entities = new List<HordeClusterEntity>();

        public HordeCluster(IHorde horde, float density, IAICommandGenerator<EntityAICommand> entityCommandGenerator)
        {
            this.horde = horde;
            this.density = new HordeClusterDensity(density);
            this.entityCommandGenerator = entityCommandGenerator;
        }

        public HordeCluster(HordeClusterData data)
        {
            this.horde = data.GetHorde();
            this.density = new HordeClusterDensity(data.GetDensity());
            this.entityCommandGenerator = data.GetEntityCommandGenerator();
        }

        public IHorde GetHorde()
        {
            return this.horde;
        }

        public void RequestSpawn(WorldHorde horde, HordeSpawnParams spawnParams, WorldHordeSpawner spawner, PlayerHordeGroup group)
        {
            if (this.Spawned && this.Spawning)
            {
                throw new InvalidOperationException("Cannot request another horde cluster spawn when already spawning.");
            }

            this.SetSpawnStateFlags(EHordeClusterSpawnState.SPAWNING);

            spawner.RequestSpawn(horde, this, group, spawnParams, () =>
            {
                this.SetSpawnStateFlags(EHordeClusterSpawnState.SPAWNED);
            });
        }

        public float GetDensity() 
        {
            return this.density.Density;
        }

        public void Decay(float dt)
        {
            const float decayRate = 0.001f;
            this.density.Remove(decayRate * dt);
        }

        public bool IsDead()
        {
            return this.density.Density <= float.Epsilon;
        }

        public void AddEntity(HordeClusterEntity entity)
        {
            this.entities.Add(entity);
            this.density.UpdateDensityPerEntity(this.entities.Count);

            if (!this.Spawned)
                this.SetSpawnStateFlags(this.spawnState | EHordeClusterSpawnState.SPAWNED);
        }

        public void RemoveEntity(HordeClusterEntity entity, bool killed)
        {
            this.entities.Remove(entity);

            if(killed)
                this.density.RemoveEntity();
        }

        public List<HordeClusterEntity> GetEntities()
        {
            return this.entities;
        }

        public void SetSpawnStateFlags(EHordeClusterSpawnState spawnState)
        {
            this.spawnState = spawnState;
        }

        private bool IsFlagSet(EHordeClusterSpawnState state)
        {
            return (this.spawnState & state) == state;
        }

        public bool Spawning // A cluster can remain in a spawned-spawning state, but a horde cannot.
        {
            get
            {
                return this.IsFlagSet(EHordeClusterSpawnState.SPAWNING);
            }
        }

        public bool Spawned
        {
            get
            {
                return this.IsFlagSet(EHordeClusterSpawnState.SPAWNED);
            }
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
            return new HordeClusterData(this.horde, this.density.Density, this.entityCommandGenerator);
        }
    }
}
