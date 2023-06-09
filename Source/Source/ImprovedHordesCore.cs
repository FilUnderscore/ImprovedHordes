using ImprovedHordes.Source.Core;
using ImprovedHordes.Source.Core.Debug;
using ImprovedHordes.Source.Core.Horde.Data;
using ImprovedHordes.Source.Core.Horde.World.Event;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde;
using ImprovedHordes.Source.POI;
using ImprovedHordes.Source.Scout;
using ImprovedHordes.Source.Wandering;
using System;
using UnityEngine;

namespace ImprovedHordes.Source
{
    public sealed class ImprovedHordesCore
    {
        private const ushort DATA_FILE_MAGIC = 0x4948;
        private const uint DATA_FILE_VERSION = 2;

        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;
        private static ImprovedHordesCore Instance;

        private bool initialized = false;
        private WorldHordeManager hordeManager;
        private WorldEventReporter worldEventReporter;
        private WorldPOIScanner poiScanner;

        private int worldSize;

        public ImprovedHordesCore(Mod mod)
        {
            Instance = this;

            this.mainThreadRequestProcessor = new MainThreadRequestProcessor();

            XPathPatcher.LoadAndPatchXMLFile(mod.Path, "Config/ImprovedHordes", "hordes.xml", xmlFile => HordesFromXml.LoadHordes(xmlFile));
        }

        public static bool TryGetInstance(out ImprovedHordesCore instance)
        {
            instance = Instance;
            return instance != null;
        }

        public void Init(World world)
        {
            if (!world.GetWorldExtent(out Vector3i minSize, out Vector3i maxSize))
            {
                throw new InvalidOperationException("Could not determine world size.");
            }

            Log.Out("[Improved Hordes] [Core] Initializing.");

            this.worldSize = maxSize.x - minSize.x;
            this.worldEventReporter = new WorldEventReporter(this.worldSize);
            this.hordeManager = new WorldHordeManager(this.mainThreadRequestProcessor, this.worldEventReporter);
            this.poiScanner = new WorldPOIScanner();

            this.hordeManager.GetPopulator().RegisterPopulator(new WorldZoneWanderingEnemyHordePopulator(this.poiScanner));
            this.hordeManager.GetPopulator().RegisterPopulator(new WorldZoneScreamerHordePopulator(this.poiScanner));

            this.hordeManager.GetPopulator().RegisterPopulator(new WorldWildernessWanderingEnemyHordePopulator(this.worldSize, this.poiScanner, new HordeSpawnData(15)));
            this.hordeManager.GetPopulator().RegisterPopulator(new WorldWildernessHordePopulator<WanderingAnimalHorde>(this.worldSize, this.poiScanner, new HordeSpawnData(15)));

            this.initialized = true;
        }

        public WorldHordeManager GetHordeManager()
        {
            return this.hordeManager;
        }

        public MainThreadRequestProcessor GetMainThreadRequestProcessor()
        {
            return this.mainThreadRequestProcessor;
        }

        public void Update()
        {
            if (!this.initialized)
                return;

            float dt = Time.fixedDeltaTime;
            MainThreaded.UpdateAll(dt);
        }

        public void Shutdown()
        {
            this.initialized = false;
        }

        public bool IsInitialized()
        {
            return this.initialized;
        }

        public int GetWorldSize()
        {
            return this.worldSize;
        }

        public WorldPOIScanner GetScanner()
        {
            return this.poiScanner;
        }
    }
}