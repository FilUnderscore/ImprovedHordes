using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.World.Horde.Spawn;
using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.Populator
{
    public sealed class WorldHordePopulator : MainThreadSynchronizedTask
    {
        private readonly WorldHordeTracker tracker;
        private readonly WorldHordeSpawner spawner;

        // Relies on object boxing, try to use reference types (i.e. classes) to avoid performance penalty.
        private readonly List<HordePopulator> populators = new List<HordePopulator>();

        public WorldHordePopulator(WorldHordeTracker tracker, WorldHordeSpawner spawner) : base()
        {
            this.tracker = tracker;
            this.spawner = spawner;
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
                if (!populator.CanRun(this.tracker))
                    continue;

                populator.Populate(dt, this.tracker, this.spawner, this.Random);
            }
        }

        public void RegisterPopulator(HordePopulator populator)
        {
            this.populators.Add(populator);
        }
    }
}
