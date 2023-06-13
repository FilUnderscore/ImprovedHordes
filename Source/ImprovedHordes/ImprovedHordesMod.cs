using HarmonyLib;
using ImprovedHordes.Core;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.Data.XML;
using ImprovedHordes.Implementations;
using ImprovedHordes.POI;
using ImprovedHordes.Screamer;
using ImprovedHordes.Wandering.Animal;
using ImprovedHordes.Wandering.Enemy.Wilderness;
using ImprovedHordes.Wandering.Enemy.Zone;

namespace ImprovedHordes
{
    public sealed class ImprovedHordesMod : IModApi
    {
        private static ImprovedHordesMod Instance;

        private ImprovedHordesCore core;
        private WorldPOIScanner poiScanner;

        public void InitMod(Mod _modInstance)
        {
            Instance = this;
            XPathPatcher.LoadAndPatchXMLFile(_modInstance, "Config/ImprovedHordes", "hordes.xml", xmlFile => HordesFromXml.LoadHordes(xmlFile));

            Harmony harmony = new Harmony("filunderscore.improvedhordes");
            harmony.PatchAll();

            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
            ModEvents.GameUpdate.RegisterHandler(GameUpdate);
            ModEvents.GameShutdown.RegisterHandler(GameShutdown);

#if EXPERIMENTAL
            new IHExperimentalManager(_modInstance);
#endif
        }

        public static bool TryGetInstance(out ImprovedHordesMod instance)
        {
            instance = Instance;

            return Instance != null;
        }

        public ImprovedHordesCore GetCore()
        {
            return this.core;
        }

        public WorldPOIScanner GetPOIScanner() 
        {
            return this.poiScanner;
        }

        private void InitializeCore(World world)
        {
            core = new ImprovedHordesCore(new ImprovedHordesEntitySpawner(), world);
            this.poiScanner = new WorldPOIScanner();

            core.GetWorldHordePopulator().RegisterPopulator(new WorldZoneWanderingEnemyHordePopulator(this.poiScanner));
            core.GetWorldHordePopulator().RegisterPopulator(new WorldZoneScreamerHordePopulator(this.poiScanner, core.GetWorldEventReporter()));

            core.GetWorldHordePopulator().RegisterPopulator(new WorldWildernessWanderingEnemyHordePopulator(core.GetWorldSize(), this.poiScanner, new HordeSpawnData(15)));
            core.GetWorldHordePopulator().RegisterPopulator(new WorldWildernessHordePopulator<WanderingAnimalHorde>(core.GetWorldSize(), this.poiScanner, new HordeSpawnData(15)));
        }

        private static void GameStartDone()
        {
            Instance.InitializeCore(GameManager.Instance.World);
        }

        private static void GameUpdate()
        {
            if (Instance.core == null)
                return;

            Instance.core.Update();
        }

        private static void GameShutdown() 
        {
            if (Instance.core == null)
                return;

            Instance.core.Shutdown();
            Instance.core = null;
        }

        [HarmonyPatch(typeof(World))]
        [HarmonyPatch("Cleanup")]
        private sealed class World_Cleanup_Patch
        {
            private static void Prefix() // Clean up on client world exit
            {
                //if (!IsHost())
                //    return;

                GameShutdown();
            }
        }
    }
}
