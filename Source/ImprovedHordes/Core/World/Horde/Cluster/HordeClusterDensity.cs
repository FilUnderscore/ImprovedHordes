namespace ImprovedHordes.Core.World.Horde.Cluster
{
    public struct HordeClusterDensity
    {
        private float density;
        private float densityPerEntity;

        public float Density 
        {
            get
            {
                return this.density;
            }
        }

        public HordeClusterDensity(float initialDensity)
        {
            this.density = initialDensity;
            this.densityPerEntity = initialDensity;
        }

        public void UpdateDensityPerEntity(int entityCount)
        {
            this.densityPerEntity = this.density / entityCount;
        }

        public void Remove(float amount)
        {
            this.density -= amount;
        }

        public void RemoveEntity()
        {
            this.density -= this.densityPerEntity;
        }
    }
}
