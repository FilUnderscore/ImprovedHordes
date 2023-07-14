using System.Collections.Generic;

namespace ImprovedHordes
{
    public sealed class IHVersionManager
    {
        private const string ISSUE_REPORT_URL = "github.com/FilUnderscore/ImprovedHordes/issues";

        private static string VERSION;

        private static string BUILD_TYPE;
        private static string EXPERIMENTAL_VERSION = "-beta.1";

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

        private void PlayerSpawnedInWorld(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos)
        {
            // Post on first player login.
            ModEvents.PlayerSpawnedInWorld.UnregisterHandler(PlayerSpawnedInWorld);

            SendChatMessage($"{VERSION + EXPERIMENTAL_VERSION} {BUILD_TYPE} Build.");

            if(TryGetAddonsListAsString(out string addonsListString))
                SendChatMessage($"Add-ons: {addonsListString}");

#if EXPERIMENTAL
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

        private static void SendChatMessage(string msg)
        {
            GameManager.Instance.ChatMessageServer(null, EChatType.Global, -1, msg, "Improved Hordes", false, null);
        }
    }
}