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

        public abstract void Populate(float dt, WorldHordeTracker tracker, WorldHordeSpawner spawner, GameRandom random);
    }

    public abstract class HordePopulator<TaskReturnValue> : HordePopulator
    {
        public override void Populate(float dt, WorldHordeTracker tracker, WorldHordeSpawner spawner, GameRandom random)
        {
            if (CanPopulate(dt, out TaskReturnValue returnValue, tracker, random))
                Populate(returnValue, spawner, random);
        }

        public abstract bool CanPopulate(float dt, out TaskReturnValue returnValue, WorldHordeTracker tracker, GameRandom random);
        public abstract void Populate(TaskReturnValue returnValue, WorldHordeSpawner spawner, GameRandom random);
    }
}
