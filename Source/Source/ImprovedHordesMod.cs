using HarmonyLib;
using System;

namespace ImprovedHordes.Source
{
    public class ImprovedHordesMod : IModApi
    {
        private static ImprovedHordesCore core;

        public void InitMod(Mod _modInstance)
        {
            core = new ImprovedHordesCore(_modInstance);

            Harmony harmony = new Harmony("filunderscore.improvedhordes");
            harmony.PatchAll();

            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
            ModEvents.GameUpdate.RegisterHandler(GameUpdate);
            ModEvents.GameShutdown.RegisterHandler(GameShutdown);

            ModEvents.EntityKilled.RegisterHandler(EntityKilled);
        }

        private static void EntityKilled(Entity killed, Entity killer)
        {
            if (core == null)
                return;

            core.GetHordeManager().Notify(killed);
        }

        private static void GameStartDone()
        {
            if (core == null)
                return;

            core.Init(GameManager.Instance.World);
        }

        private static void GameUpdate()
        {
            if (core == null)
                return;

            core.Update();
        }

        private static void GameShutdown() 
        {
            if (core == null)
                return;

            core.Shutdown();
        }
    }
}
