using ImprovedHordes.Core.Abstractions.Settings;
using System.Collections.Generic;

namespace ImprovedHordes
{
    public sealed class IHVersionManager
    {
        private static Setting<bool> SILENCE_INIT_MSG = new Setting<bool>("silence_init_msg", false);

        private static string VERSION = "-beta.5";
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
            VERSION = _modInstance.Version.ToString() + VERSION;

#if !RELEASE
            Log.Out($"[Improved Hordes] Currently running version {VERSION}.");
#endif

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

            SendChatMessage($"{VERSION} {BUILD_TYPE} Build.");

            if(TryGetAddonsListAsString(out string addonsListString))
                SendChatMessage($"{addonsListString}", "Add-ons");

#if EXPERIMENTAL
            const string ISSUE_REPORT_URL = "github.com/FilUnderscore/ImprovedHordes/issues";
            SendChatMessage($"Please report any bugs/performance issues at {ISSUE_REPORT_URL}");
#endif
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

        private static void SendChatMessage(string msg, string name = "Improved Hordes")
        {
            if (!SILENCE_INIT_MSG.Value)
            {
                GameManager.Instance.ChatMessageServer(null, EChatType.Global, -1, msg, name, false, null);
            }
            else
            {
                Log.Out($"[{name}] {msg}");
            }
        }
    }
}