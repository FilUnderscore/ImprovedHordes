using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Event;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Populator;
using ImprovedHordes.Core.World.Horde.Spawn;
using UnityEngine;

namespace ImprovedHordes.Core
{
    public sealed class ImprovedHordesCore
    {
        private const ushort DATA_FILE_MAGIC = 0x4948;
        private const uint DATA_FILE_VERSION = 8;

        private readonly ILoggerFactory loggerFactory;
        private readonly IRandomFactory<IWorldRandom> randomFactory;
        private readonly Abstractions.Logging.ILogger logger;

        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;

        private readonly WorldHordeTracker tracker;
        private readonly WorldHordeSpawner spawner;
        private readonly WorldHordePopulator populator;

        private readonly WorldEventReporter worldEventReporter;
        
        private readonly int worldSize;

        public ImprovedHordesCore(int worldSize, ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory, IEntitySpawner entitySpawner)
        {
            this.loggerFactory = loggerFactory;
            this.randomFactory = randomFactory;
            this.logger = loggerFactory.Create(typeof(ImprovedHordesCore));

            this.mainThreadRequestProcessor = new MainThreadRequestProcessor();

            this.logger.Info("Initializing.");

            this.worldSize = worldSize;
            this.worldEventReporter = new WorldEventReporter(loggerFactory, randomFactory, this.worldSize);

            this.tracker = new WorldHordeTracker(loggerFactory, randomFactory, entitySpawner, this.mainThreadRequestProcessor, this.worldEventReporter);
            this.spawner = new WorldHordeSpawner(loggerFactory, randomFactory, this.tracker, this.mainThreadRequestProcessor);
            this.populator = new WorldHordePopulator(loggerFactory, randomFactory, this.tracker, this.spawner, this.worldSize);
        }

        public void Start()
        {
            Threaded.StartAll();
        }

        public bool Load(IDataLoader dataLoader) 
        {
            ushort loaded_data_magic = dataLoader.Load<ushort>();
            uint loaded_data_version = dataLoader.Load<uint>();

            if(loaded_data_magic != DATA_FILE_MAGIC)
            {
                this.logger.Warn($"Data file magic mismatch. Expected {DATA_FILE_MAGIC}, read {loaded_data_magic}.");
                return false;
            }
            else if(loaded_data_version < DATA_FILE_VERSION)
            {
                this.logger.Warn($"Data file version has changed. Previous version {loaded_data_version} < current version {DATA_FILE_VERSION}.");
                return false;
            }

            this.tracker.Load(dataLoader);
            this.populator.Load(dataLoader);

            return true;
        }

        public void Save(IDataSaver dataSaver) 
        {
            dataSaver.Save<ushort>(DATA_FILE_MAGIC);
            dataSaver.Save<uint>(DATA_FILE_VERSION);

            this.tracker.Save(dataSaver);
            this.populator.Save(dataSaver);
        }

        public void Flush()
        {
            this.tracker.Flush();
            this.populator.Flush();
        }

        public ILoggerFactory GetLoggerFactory()
        {
            return this.loggerFactory;
        }

        public IRandomFactory<IWorldRandom> GetRandomFactory()
        {
            return this.randomFactory;
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

            Threaded.ShutdownAll();
            MainThreaded.ShutdownAll();
        }
    }
}