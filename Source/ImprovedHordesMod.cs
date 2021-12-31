﻿using HarmonyLib;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes
{
    /* Licensed under CC-BY-SA 4.0 - (C) FilUnderscore 2021.
     * More information can be found in LICENSE file in the root of the repository. 
     */
    class ImprovedHordesMod : IModApi
    {
        private static ImprovedHordesManager manager;

        public void InitMod(Mod mod)
        {
            manager = new ImprovedHordesManager(mod);

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

            if(manager != null)
                manager.Init();
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
