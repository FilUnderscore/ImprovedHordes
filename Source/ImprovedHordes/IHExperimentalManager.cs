#if EXPERIMENTAL || DEBUG
namespace ImprovedHordes
{
    public sealed class IHExperimentalManager
    {
        private const string ISSUE_REPORT_URL = "github.com/FilUnderscore/ImprovedHordes/issues";

        private static string VERSION;

        private static string BUILD_TYPE;
        private static string EXPERIMENTAL_VERSION = "-alpha.8";

        static IHExperimentalManager()
        {
#if DEBUG
            BUILD_TYPE = "Debug";
#elif EXPERIMENTAL
            BUILD_TYPE = "Experimental";
#endif
        }

        public IHExperimentalManager(Mod _modInstance)
        {
            VERSION = _modInstance.Version.ToString();

            ModEvents.PlayerSpawnedInWorld.RegisterHandler(PlayerSpawnedInWorld);
        }

        private static void PlayerSpawnedInWorld(ClientInfo clientInfo, RespawnType respawnType, Vector3i pos)
        {
            // Post on first player login.
            ModEvents.PlayerSpawnedInWorld.UnregisterHandler(PlayerSpawnedInWorld);

            GameManager.Instance.ChatMessageServer(null, EChatType.Global, -1, $"{VERSION + EXPERIMENTAL_VERSION} {BUILD_TYPE} Build.", "Improved Hordes", false, null);
            GameManager.Instance.ChatMessageServer(null, EChatType.Global, -1, $"Please report any bugs/performance issues at {ISSUE_REPORT_URL}", "Improved Hordes", false, null);
        }
    }
}
#endif