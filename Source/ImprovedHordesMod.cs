using HarmonyLib;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes
{
    class ImprovedHordesMod : IModApi
    {
        private static ImprovedHordesManager manager;
        private static bool host;

        public void InitMod(Mod mod)
        {
            manager = new ImprovedHordesManager(mod);

            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
            ModEvents.GameUpdate.RegisterHandler(GameUpdate);

            ModEvents.EntityKilled.RegisterHandler(EntityKilled);

            ModEvents.PlayerDisconnected.RegisterHandler(PlayerDisconnected);

            this.HarmonyPatch();
        }

        private void HarmonyPatch()
        {
            var harmony = new Harmony("filunderscore.improvedhordes");
            harmony.PatchAll();
        }

        public static bool IsHost()
        {
            return host;
        }

        static void GameStartDone()
        {
            host = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;

            if (!IsHost())
                return;

            Log("Initializing.");

            if (manager != null)
                manager.Init();
        }

        static void GameUpdate()
        {
            if (!IsHost())
                return;

            if (manager != null)
                manager.Update();
        }

        static void EntityKilled(Entity killed, Entity killer)
        {
            if (!IsHost())
                return;

            manager.EntityKilled(killed, killer);
        }

        static void PlayerDisconnected(ClientInfo cInfo, bool shutdown)
        {
            if (!IsHost())
                return;

            int playerId;

            if (cInfo != null)
            {
                playerId = cInfo.entityId;
            }
            else
            {
                playerId = manager.World.GetPrimaryPlayerId();
            }

            if (manager != null)
                manager.RemovePlayer(playerId);
        }

        class HarmonyPatches
        {
            [HarmonyPatch(typeof(global::World))]
            [HarmonyPatch("Save")]
            class WorldSaveHook
            {
                static void Postfix() // Save on world save
                {
                    if (!IsHost())
                        return;

                    if (manager != null && manager.Initialized())
                        manager.Save();
                }
            }

            [HarmonyPatch(typeof(global::World))]
            [HarmonyPatch("Cleanup")]
            class WorldCleanupHook
            {
                static void Prefix() // Clean up on client disconnect
                {
                    if (!IsHost())
                        return;

                    if (manager != null && manager.Initialized())
                        manager.Shutdown();
                }
            }
        }
    }
}
