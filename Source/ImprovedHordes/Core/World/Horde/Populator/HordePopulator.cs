using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.Populator
{
    public abstract class HordePopulator : IData
    {
        public virtual bool CanRun(List<PlayerHordeGroup> playerGroups, Dictionary<Type,List<ClusterSnapshot>> clusters)
        {
            return true;
        }

        public abstract void Populate(float dt, List<PlayerHordeGroup> playerGroups, Dictionary<Type, List<ClusterSnapshot>> clusters, WorldHordeSpawner spawner, IWorldRandom worldRandom);

        public abstract IData Load(IDataLoader loader);
        public abstract void Save(IDataSaver saver);

        public abstract void Flush();
    }

    public abstract class HordePopulator<TaskReturnValue> : HordePopulator
    {
        public override void Populate(float dt, List<PlayerHordeGroup> playerGroups, Dictionary<Type, List<ClusterSnapshot>> clusters, WorldHordeSpawner spawner, IWorldRandom worldRandom)
        {
            if (CanPopulate(dt, out TaskReturnValue returnValue, playerGroups, clusters, worldRandom))
                Populate(returnValue, spawner, worldRandom);
        }

        public abstract bool CanPopulate(float dt, out TaskReturnValue returnValue, List<PlayerHordeGroup> playerGroups, Dictionary<Type, List<ClusterSnapshot>> clusters, IWorldRandom worldRandom);
        public abstract void Populate(TaskReturnValue returnValue, WorldHordeSpawner spawner, IWorldRandom worldRandom);
    }
}
