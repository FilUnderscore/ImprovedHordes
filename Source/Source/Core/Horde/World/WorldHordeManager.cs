using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Event;
using ImprovedHordes.Source.Core.Horde.World.Spawn;

namespace ImprovedHordes.Source.Horde
{
    public sealed class WorldHordeManager
    {
        private readonly WorldHordeTracker tracker;
        private readonly WorldHordeSpawner spawner;

        public WorldHordeManager(WorldEventReporter reporter)
        {
            this.tracker = new WorldHordeTracker(reporter);
            this.spawner = new WorldHordeSpawner(this.tracker);
        }

        public WorldHordeSpawner GetSpawner()
        {
            return this.spawner;
        }

        public WorldHordeTracker GetTracker()
        {
            return this.tracker;
        }

        public void Update(float dt)
        {
            this.tracker.Update(dt);
        }
    }
}