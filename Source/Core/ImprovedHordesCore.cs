using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Event;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Populator;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using UnityEngine;

namespace ImprovedHordes.Core
{
    public sealed class ImprovedHordesCore
    {
        private const ushort DATA_FILE_MAGIC = 0x4948;
        private const uint DATA_FILE_VERSION = 2;

        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;

        private readonly WorldHordeTracker tracker;
        private readonly WorldHordeSpawner spawner;
        private readonly WorldHordePopulator populator;

        private readonly WorldEventReporter worldEventReporter;
        
        private readonly int worldSize;

        public ImprovedHordesCore(global::World world)
        {
            this.mainThreadRequestProcessor = new MainThreadRequestProcessor();

            Log.Out("[Improved Hordes] [Core] Initializing.");

            if (!world.GetWorldExtent(out Vector3i minSize, out Vector3i maxSize))
            {
                throw new InvalidOperationException("Could not determine world size.");
            }

            this.worldSize = maxSize.x - minSize.x;
            this.worldEventReporter = new WorldEventReporter(this.worldSize);

            this.tracker = new WorldHordeTracker(this.mainThreadRequestProcessor, this.worldEventReporter);
            this.spawner = new WorldHordeSpawner(this.tracker);
            this.populator = new WorldHordePopulator(this.tracker, this.spawner);
        }

        public WorldHordePopulator GetWorldHordePopulator()
        {
            return this.populator;
        }

        public WorldHordeSpawner GetWorldHordeSpawner()
        {
            return this.spawner;
        }

        public WorldHordeTracker GetWorldHordeTracker() 
        {
            return this.tracker;
        }

        public WorldEventReporter GetWorldEventReporter() 
        {
            return this.worldEventReporter;
        }

        public MainThreadRequestProcessor GetMainThreadRequestProcessor()
        {
            return this.mainThreadRequestProcessor;
        }

        public int GetWorldSize()
        {
            return this.worldSize;
        }

        public void Update()
        {
            float dt = Time.fixedDeltaTime;
            MainThreaded.UpdateAll(dt);
        }

        public void Shutdown()
        {
            MainThreaded.ShutdownAll();
        }
    }
}