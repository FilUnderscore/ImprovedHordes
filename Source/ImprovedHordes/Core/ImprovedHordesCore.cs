using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.World;
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

        private readonly ILoggerFactory loggerFactory;
        private readonly Abstractions.Logging.ILogger logger;

        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;

        private readonly WorldHordeTracker tracker;
        private readonly WorldHordeSpawner spawner;
        private readonly WorldHordePopulator populator;

        private readonly WorldEventReporter worldEventReporter;
        
        private readonly int worldSize;

        public ImprovedHordesCore(ILoggerFactory loggerFactory, IEntitySpawner entitySpawner, global::World world)
        {
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.Create(typeof(ImprovedHordesCore));

            this.mainThreadRequestProcessor = new MainThreadRequestProcessor();

            this.logger.Info("Initializing.");

            if (!world.GetWorldExtent(out Vector3i minSize, out Vector3i maxSize))
            {
                throw new InvalidOperationException("Could not determine world size.");
            }

            this.worldSize = maxSize.x - minSize.x;
            this.worldEventReporter = new WorldEventReporter(loggerFactory, this.worldSize);

            this.tracker = new WorldHordeTracker(loggerFactory, entitySpawner, this.mainThreadRequestProcessor, this.worldEventReporter);
            this.spawner = new WorldHordeSpawner(this.tracker);
            this.populator = new WorldHordePopulator(loggerFactory, this.tracker, this.spawner);
        }

        public ILoggerFactory GetLoggerFactory()
        {
            return this.loggerFactory;
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
            this.logger.Info("Shutting down.");

            MainThreaded.ShutdownAll();
        }
    }
}