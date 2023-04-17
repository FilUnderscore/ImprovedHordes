using ImprovedHordes.Source.Core;
using ImprovedHordes.Source.Core.Horde.Data;
using ImprovedHordes.Source.Core.Horde.World.Event;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde;
using ImprovedHordes.Source.POI;
using ImprovedHordes.Source.Wandering;
using System;
using UnityEngine;

namespace ImprovedHordes.Source
{
    public class ImprovedHordesCore
    {
        private const ushort DATA_FILE_MAGIC = 0x4948;
        private const uint DATA_FILE_VERSION = 2;

        private static ImprovedHordesCore Instance;

        private bool initialized = false;
        private WorldHordeManager hordeManager;
        private MainThreadRequestProcessor mainThreadRequestProcessor;
        private WorldEventReporter worldEventReporter;
        private WorldPOIScanner poiScanner;
        private WorldWanderingHordePopulator wanderingHordePopulator;

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

            this.worldEventReporter = new WorldEventReporter(maxSize.x - minSize.x);
            this.hordeManager = new WorldHordeManager(this.worldEventReporter);
            this.poiScanner = new WorldPOIScanner();
            this.wanderingHordePopulator = new WorldWanderingHordePopulator(this.poiScanner, this.hordeManager.GetTracker(), this.hordeManager.GetSpawner());

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

            this.mainThreadRequestProcessor.Update();
            this.hordeManager.Update(dt);
            this.worldEventReporter.Update();
            this.wanderingHordePopulator.Update();
        }

        public void Shutdown()
        {
            this.initialized = false;
        }
    }
}