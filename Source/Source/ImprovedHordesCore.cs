using ImprovedHordes.Source.Core.Horde.World.Event;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde;
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

        public ImprovedHordesCore(Mod mod)
        {
            Instance = this;

            this.mainThreadRequestProcessor = new MainThreadRequestProcessor();
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

            this.hordeManager = new WorldHordeManager();
            this.worldEventReporter = new WorldEventReporter(maxSize.x - minSize.x);
            this.initialized = true;

            this.worldEventReporter.OnWorldEventReport += WorldEventReporter_OnWorldEventReport;
        }

        private void WorldEventReporter_OnWorldEventReport(object sender, WorldEventReportEvent e)
        {
            Log.Out($"Pos {e.GetLocation()} Interest {e.GetInterest()} Dist {e.GetDistance()}");
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
        }

        public void Shutdown()
        {
        }
    }
}