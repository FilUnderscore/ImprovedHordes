using HarmonyLib;

using ImprovedHordes.Horde;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes
{
    class ImprovedHordesMod : IModApi
    {
        private static HordeManager manager;

        public void InitMod(Mod mod)
        {
            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
            ModEvents.GameUpdate.RegisterHandler(GameUpdate);
            ModEvents.GameShutdown.RegisterHandler(GameShutdown);

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

            manager = new HordeManager();
            manager.Load();
        }

        static void GameUpdate()
        {
            if (manager != null)
                manager.Update();
        }

        static void GameShutdown()
        {
            if (manager != null)
                manager.Shutdown();
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
    }
}
