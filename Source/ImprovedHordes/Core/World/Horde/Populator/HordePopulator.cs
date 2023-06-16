using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using System.Collections.Generic;
using static ImprovedHordes.Core.World.Horde.WorldHordeTracker;

namespace ImprovedHordes.Core.World.Horde.Populator
{
    public abstract class HordePopulator
    {
        public virtual bool CanRun(List<PlayerSnapshot> players, Dictionary<Type,List<ClusterSnapshot>> clusters)
        {
            return true;
        }

        public abstract void Populate(float dt, List<PlayerSnapshot> players, Dictionary<Type, List<ClusterSnapshot>> clusters, WorldHordeSpawner spawner, GameRandom random);
    }

    public abstract class HordePopulator<TaskReturnValue> : HordePopulator
    {
        public override void Populate(float dt, List<PlayerSnapshot> players, Dictionary<Type, List<ClusterSnapshot>> clusters, WorldHordeSpawner spawner, GameRandom random)
        {
            if (CanPopulate(dt, out TaskReturnValue returnValue, players, clusters, random))
                Populate(returnValue, spawner, random);
        }

        public abstract bool CanPopulate(float dt, out TaskReturnValue returnValue, List<PlayerSnapshot> players, Dictionary<Type, List<ClusterSnapshot>> clusters, GameRandom random);
        public abstract void Populate(TaskReturnValue returnValue, WorldHordeSpawner spawner, GameRandom random);
    }
}
