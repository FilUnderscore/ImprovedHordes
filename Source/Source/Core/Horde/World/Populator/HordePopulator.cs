using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Spawn;

namespace ImprovedHordes.Source.Core.Horde.World.Populator
{
    public abstract class HordePopulator
    {
        public virtual bool CanRun(WorldHordeTracker tracker)
        {
            return true;
        }

        public abstract void Populate(float dt, WorldHordeTracker tracker, WorldHordeSpawner spawner);
    }

    public abstract class HordePopulator<TaskReturnValue> : HordePopulator
    {
        public override void Populate(float dt, WorldHordeTracker tracker, WorldHordeSpawner spawner)
        {
            if (CanPopulate(dt, out TaskReturnValue returnValue, tracker))
                Populate(returnValue, spawner);
        }

        public abstract bool CanPopulate(float dt, out TaskReturnValue returnValue, WorldHordeTracker tracker);
        public abstract void Populate(TaskReturnValue returnValue, WorldHordeSpawner spawner);
    }
}
