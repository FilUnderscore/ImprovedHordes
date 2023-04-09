using ImprovedHordes.Source.Core.Horde.World;
using ImprovedHordes.Source.Core.Horde.World.LOI;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
using ImprovedHordes.Source.Horde.World.LOI;

namespace ImprovedHordes.Source.Horde
{
    public sealed class WorldHordeManager
    {
        private readonly AIExecutor aiExecutor;

        private readonly WorldHordeClusterTracker worldHordeClusterTracker;
        private readonly WorldHordeSpawner spawner;

        public WorldHordeManager(WorldLOITracker worldLOITracker)
        {
            this.aiExecutor = new AIExecutor();
            this.worldHordeClusterTracker = new WorldHordeClusterTracker(this, this.aiExecutor);
            this.spawner = new WorldHordeSpawner(this.worldHordeClusterTracker);

            this.worldHordeClusterTracker.ExecuteThread();

            worldLOITracker.OnInterestNotificationEventThread += WorldLOITracker_OnInterestNotification;
        }

        public WorldHordeSpawner GetSpawner()
        {
            return this.spawner;
        }

        public string GetName()
        {
            return "IH-WorldHordeManager";
        }

        public void Update(float dt)
        {
            this.spawner.Update();
            this.aiExecutor.Update(dt);
        }

        private void WorldLOITracker_OnInterestNotification(object sender, LOIInterestNotificationEvent e)
        {
            this.worldHordeClusterTracker.NotifyHordeClustersNearby(e.GetLocation(), e.GetDistance());
        }

        public void Shutdown()
        {
            this.worldHordeClusterTracker.ShutdownThread();
        }
    }
}