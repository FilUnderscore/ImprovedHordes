using HarmonyLib;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes
{
    class ImprovedHordesMod : IModApi
    {
        private static ImprovedHordesManager manager;

        public void InitMod(Mod mod)
        {
            manager = new ImprovedHordesManager(mod);

            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
            ModEvents.GameUpdate.RegisterHandler(GameUpdate);
            
            ModEvents.EntityKilled.RegisterHandler(EntityKilled);

            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
            ModEvents.PlayerDisconnected.RegisterHandler(PlayerDisconnected);

            this.HarmonyPatch();
        }

        private void HarmonyPatch()
        {
            var harmony = new Harmony("filunderscore.improvedhordes");
            harmony.PatchAll();
        }

        static void GameStartDone()
        {
            Log("Initializing.");

            if(manager != null)
                manager.Init();
        }

        static void GameUpdate()
        {
            if (manager != null)
                manager.Update();
        }

        static void EntityKilled(Entity killed, Entity killer)
        {
            manager.EntityKilled(killed, killer);
        }

        static void PlayerSpawnedInWorld(ClientInfo cInfo, RespawnType respawnType, Vector3i pos)
        {
            if (manager != null)
            {
                int playerId;

                if (cInfo != null) // Multiplayer.
                {
                    playerId = cInfo.entityId;
                }
                else
                {
                    playerId = manager.World.GetPrimaryPlayerId(); // Singleplayer.
                }

                switch (respawnType)
                {
                    case RespawnType.EnterMultiplayer:
                    case RespawnType.JoinMultiplayer:
                    case RespawnType.NewGame:
                    case RespawnType.LoadedGame:
                        manager.AddPlayer(playerId);
                        break;
                }
            }
        }

        static void PlayerDisconnected(ClientInfo cInfo, bool shutdown)
        {
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
            [HarmonyPatch(typeof(World))]
            [HarmonyPatch("Save")]
            class WorldSaveHook
            {
                static void Postfix() // Save on world save
                {
                    if (manager != null && manager.Initialized())
                        manager.Save();
                }
            }

            [HarmonyPatch(typeof(World))]
            [HarmonyPatch("Cleanup")]
            class WorldCleanupHook
            {
                static void Prefix() // Clean up on client disconnect
                {
                    if (manager != null && manager.Initialized())
                        manager.Shutdown();
                }
            }
        }
    }
}
