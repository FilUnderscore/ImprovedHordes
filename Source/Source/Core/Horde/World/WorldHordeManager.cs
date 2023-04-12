using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;

namespace ImprovedHordes.Source.Horde
{
    public sealed class WorldHordeManager
    {
        private readonly AIExecutor aiExecutor;

        private readonly WorldHordeClusterTracker clusterTracker;
        private readonly WorldHordeSpawner spawner;

        public WorldHordeManager()
        {
            this.aiExecutor = new AIExecutor();
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
            this.clusterTracker.Update();
            this.spawner.Update();

            this.aiExecutor.Update(dt);
        }
    }
}