using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using System.Collections.Generic;
using static ImprovedHordes.Core.World.Horde.WorldHordeTracker;

namespace ImprovedHordes.Core.World.Horde.Populator
{
    public abstract class HordePopulator
    {
        protected readonly ThreadSubscriber<List<PlayerSnapshot>> Players;
        protected readonly ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> Clusters;

        public HordePopulator(WorldHordeTracker tracker)
        {
            this.Players = tracker.GetPlayersSubscription().Subscribe();
            this.Clusters = tracker.GetClustersSubscription().Subscribe();
        }

        public virtual bool CanRun(WorldHordeTracker tracker)
        {
            return true;
        }

        public abstract void Populate(float dt, WorldHordeTracker tracker, WorldHordeSpawner spawner, GameRandom random);
    }

    public abstract class HordePopulator<TaskReturnValue> : HordePopulator
    {
        protected HordePopulator(WorldHordeTracker tracker) : base(tracker)
        {
        }

        public override void Populate(float dt, WorldHordeTracker tracker, WorldHordeSpawner spawner, GameRandom random)
        {
            if (CanPopulate(dt, out TaskReturnValue returnValue, tracker, random))
                Populate(returnValue, spawner, random);
        }

        public abstract bool CanPopulate(float dt, out TaskReturnValue returnValue, WorldHordeTracker tracker, GameRandom random);
        public abstract void Populate(TaskReturnValue returnValue, WorldHordeSpawner spawner, GameRandom random);
    }
}
