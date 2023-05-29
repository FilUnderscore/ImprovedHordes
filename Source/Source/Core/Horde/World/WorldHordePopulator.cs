using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Core.Threading;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public abstract class WorldHordePopulator<TaskReturnValue> : MainThreadSynchronizedTask<TaskReturnValue>
    {
        protected readonly WorldHordeTracker tracker;
        protected readonly WorldHordeSpawner spawner;

        public WorldHordePopulator(WorldHordeTracker tracker, WorldHordeSpawner spawner)
        {
            this.tracker = tracker;
            this.spawner = spawner;
        }
    }
}
