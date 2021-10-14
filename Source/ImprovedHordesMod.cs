using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using ImprovedHordes.Horde;
using static ImprovedHordes.IHLog;

namespace ImprovedHordes
{
    class ImprovedHordesMod : IModApi
    {
        public static HordeManager manager;

        public void InitMod()
        {
            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
            ModEvents.GameUpdate.RegisterHandler(GameUpdate);
            ModEvents.GameShutdown.RegisterHandler(GameShutdown);

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
                manager.Save();
        }

        static void PlayerSpawnedInWorld(ClientInfo cInfo, RespawnType respawnType, Vector3i pos)
        {
            if (cInfo == null)
                Error("Null client.");

            if (manager != null)
            {
                int playerId = cInfo.entityId;

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
            if (cInfo == null)
                Error("Null client.");

            int playerId = cInfo.entityId;

            if(manager != null)
                manager.RemovePlayer(playerId);
        }
    }
}
