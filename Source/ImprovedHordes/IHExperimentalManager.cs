#if EXPERIMENTAL
namespace ImprovedHordes
{
    public sealed class IHExperimentalManager
    {
        private const string ISSUE_REPORT_URL = "github.com/FilUnderscore/ImprovedHordes/issues";

        private static string VERSION;

        public IHExperimentalManager(Mod _modInstance)
        {
            VERSION = _modInstance.Version.ToString();

            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
        }

        private static void PlayerSpawnedInWorld(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos)
        {
            // Post on first player login.
            ModEvents.PlayerSpawnedInWorld.UnregisterHandler(PlayerSpawnedInWorld);

            GameManager.Instance.ChatMessageServer(null, EChatType.Global, -1, $"{VERSION} Experimental Build.", "Improved Hordes", false, null);
            GameManager.Instance.ChatMessageServer(null, EChatType.Global, -1, $"Please report any bugs/performance issues at - {ISSUE_REPORT_URL}", "Improved Hordes", false, null);
        }
    }
}
#endif