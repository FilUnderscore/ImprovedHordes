namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class HordeCluster
    {
        private readonly IHorde horde;

        private float density;
        private float densityPerEntity;

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
            return this.density < 0.1f;
        }

        public void SetMaxEntityCount(int maxEntityCount)
        {
            if (maxEntityCount == 0)
                return;

            this.densityPerEntity = this.density / maxEntityCount;
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
