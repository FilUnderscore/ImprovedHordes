using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Spawn;

namespace ImprovedHordes.Source.Horde
{
    public sealed class WorldHordeManager
    {
        private readonly WorldHordeClusterTracker clusterTracker;
        private readonly WorldHordeSpawner spawner;

        public WorldHordeManager()
        {
            this.clusterTracker = new WorldHordeClusterTracker();
            this.spawner = new WorldHordeSpawner(this.clusterTracker);

            //worldLOITracker.OnInterestNotificationEventThread += WorldLOITracker_OnInterestNotification;
        }

        public WorldHordeSpawner GetSpawner()
        {
            return this.spawner;
        }

        public WorldHordeClusterTracker GetClusterTracker()
        {
            return this.clusterTracker;
        }

        public void Update(float dt)
        {
            this.clusterTracker.Update(dt);
            this.spawner.Update();
        }
    }
}