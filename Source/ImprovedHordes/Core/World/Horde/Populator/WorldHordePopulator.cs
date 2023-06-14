using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using System.Collections.Generic;
using static ImprovedHordes.Core.World.Horde.WorldHordeTracker;

namespace ImprovedHordes.Core.World.Horde.Populator
{
    public sealed class WorldHordePopulator : MainThreadSynchronizedTask
    {
        private readonly ThreadSubscriber<List<PlayerSnapshot>> players;
        private readonly ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> clusters;

        private readonly WorldHordeSpawner spawner;

        private readonly List<HordePopulator> populators = new List<HordePopulator>();

        public WorldHordePopulator(ILoggerFactory loggerFactory, WorldHordeTracker tracker, WorldHordeSpawner spawner) : base(loggerFactory)
        {
            this.spawner = spawner;

            this.players = tracker.GetPlayersSubscription().Subscribe();
            this.clusters = tracker.GetClustersSubscription().Subscribe();
        }

        protected override void BeforeTaskRestart()
        {
        }

        protected override void OnTaskFinish()
        {
        }

        protected override void UpdateAsyncVoid(float dt)
        {
            foreach(var populator in this.populators)
            {
                if (!populator.CanRun(this.players, this.clusters))
                    continue;

                populator.Populate(dt, this.players, this.clusters, this.spawner, this.Random);
            }
        }

        public void RegisterPopulator(HordePopulator populator)
        {
            this.populators.Add(populator);
        }
    }
}
