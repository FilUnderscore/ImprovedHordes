using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.Cluster
{
    public sealed class HordeCluster
    {
        private readonly IHorde horde;
        private HordeEntityGenerator previousHordeEntityGenerator;

        private float density;
        private float densityPerEntity;

        private readonly List<HordeClusterEntity> entities = new List<HordeClusterEntity>();
        private bool spawned;

        public HordeCluster(IHorde horde, float density)
        {
            this.horde = horde;
            this.density = density;
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

        public bool IsDead()
        {
            return this.density <= float.Epsilon;
        }

        public void AddEntity(HordeClusterEntity entity)
        {
            this.entities.Add(entity);
            this.densityPerEntity = this.density / this.entities.Count;
        }

        public void RemoveEntity(HordeClusterEntity entity) 
        {
            this.entities.Remove(entity);
        }

        public List<HordeClusterEntity> GetEntities()
        {
            return this.entities;
        }

        public void SetSpawned(bool spawned)
        {
            this.spawned = spawned;
        }

        public bool IsSpawned()
        {
            return this.spawned;
        }
    }
}
