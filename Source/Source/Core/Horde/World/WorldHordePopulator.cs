using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Spawn;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public class WorldHordePopulator
    {
        protected readonly WorldHordeTracker tracker;
        protected readonly WorldHordeSpawner spawner;

        public WorldHordePopulator(WorldHordeTracker tracker, WorldHordeSpawner spawner)
        {
            this.tracker = tracker;
            this.spawner = spawner;
        }

        public virtual void Update()
        {

        }
    }
}
