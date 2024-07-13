using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes
{
    public sealed class IHVersionManager
    {
        private static string VERSION;
        private static string BUILD_TYPE;

        private readonly List<Mod> addons = new List<Mod>();

        static IHVersionManager()
        {
#if DEBUG
            BUILD_TYPE = "Debug";
#elif EXPERIMENTAL
            BUILD_TYPE = "Experimental";
#elif RELEASE
            BUILD_TYPE = "Stable";
#endif
        }

        public IHVersionManager(ImprovedHordesMod mod, Mod _modInstance)
        {
            VERSION = _modInstance.Version.ToString();

            mod.OnFirstInit += Mod_OnFirstInit;
        }

        public void RegisterAddonMod(Mod mod)
        {
            if(addons.Contains(mod)) 
                return;

            addons.Add(mod);
        }

        private void Mod_OnFirstInit(object sender, System.EventArgs e)
        {
            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
        }

        // Called on first time initialization of a world/update.
        private void PlayerSpawnedInWorld(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos)
        {
            // Post on first player login.
            ModEvents.PlayerSpawnedInWorld.UnregisterHandler(PlayerSpawnedInWorld);

            GameManager.Instance.StartCoroutine(NotifyAllCoroutine());

            //if(TryGetAddonsListAsString(out string addonsListString))
            //    SendChatMessage($"{addonsListString}", "Add-ons");
        }

        private bool TryGetAddonsListAsString(out string str)
        {
            str = "";

            if (addons.Count == 0)
                return false;

            str = addons[0].DisplayName;
            for(int i = 1; i < addons.Count; i++)
            {
                str += $", {addons[i].DisplayName}";
            }

            return true;
        }

        public int GetAddonListHashCode()
        {
            int hashCode = 0;

            foreach(var addon in addons)
            {
                hashCode += addon.Name.GetHashCode() * addon.VersionString.GetHashCode();
            }

            return hashCode;
        }

        private static IEnumerator NotifyAllCoroutine()
        {
            yield return new WaitForSeconds(10.0f);

            NotifyAll($"Initialized Improved Hordes ({VERSION} {BUILD_TYPE}).");

#if EXPERIMENTAL
            const string ISSUE_REPORT_URL = "github.com/FilUnderscore/ImprovedHordes/issues";
            NotifyAll($"Please report any bugs/performance issues at {ISSUE_REPORT_URL}");
#endif

            yield return null;
        }

        private static void NotifyAll(string msg, string name = "Improved Hordes")
        {
            Log.Out($"[{name}] {msg}");

            foreach (var player in GameManager.Instance.World.Players.list)
            {
                GameManager.ShowTooltipMP(player, msg);
            }
        }
    }
}