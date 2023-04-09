using ImprovedHordes.Source.Core.Horde.World;
using ImprovedHordes.Source.Core.Horde.World.LOI;
using ImprovedHordes.Source.Horde;
using ImprovedHordes.Source.Utils;
using System;
using UnityEngine;

namespace ImprovedHordes.Source
{
    public class ImprovedHordesCore
    {
        private const ushort DATA_FILE_MAGIC = 0x4948;
        private const uint DATA_FILE_VERSION = 2;

        private static ImprovedHordesCore Instance;

        private WorldLOITracker worldLOITracker;
        private WorldHordeManager hordeManager;

        public ImprovedHordesCore(Mod mod)
        {
            Instance = this;
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

            this.worldLOITracker = new WorldLOITracker(maxSize.x - minSize.x);
            this.hordeManager = new WorldHordeManager(this.worldLOITracker);
        }

        public WorldHordeManager GetHordeManager()
        {
            return this.hordeManager;
        }

        public void Update()
        {
            float dt = Time.fixedDeltaTime;

            this.worldLOITracker.Update();
            this.hordeManager.Update(dt);
        }

        public void Shutdown()
        {
            this.hordeManager.Shutdown();
            this.worldLOITracker.Shutdown();
        }
    }
}