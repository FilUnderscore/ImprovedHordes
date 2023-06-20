using HarmonyLib;
using ImprovedHordes.Core;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.Data.XML;
using ImprovedHordes.Implementations.Logging;
using ImprovedHordes.Implementations.World;
using ImprovedHordes.POI;
using ImprovedHordes.Screamer;
using ImprovedHordes.Wandering.Animal;
using ImprovedHordes.Wandering.Enemy.Wilderness;
using ImprovedHordes.Wandering.Enemy.Zone;
using ImprovedHordes.Core.Abstractions.Logging;
using System;
using ImprovedHordes.Implementations.World.Random;
using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Implementations.Settings;
using ImprovedHordes.Implementations.Settings.Parsers;
using ImprovedHordes.Wandering.Animal.Enemy;
using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Implementations.Data;
using System.IO;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Abstractions.Random;

namespace ImprovedHordes
{
    public sealed class ImprovedHordesMod : IModApi
    {
        private static ImprovedHordesMod Instance;

        private Mod modInstance;

        private readonly Harmony harmony;

        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;

        private IDataParserRegistry dataParserRegistry;

        private ISettingLoader settingLoader;

        private ImprovedHordesCore core;
        private WorldPOIScanner poiScanner;

        public ImprovedHordesMod()
        {
            this.harmony = new Harmony("filunderscore.improvedhordes");
            this.loggerFactory = new ImprovedHordesLoggerFactory();
            this.logger = this.loggerFactory.Create(typeof(ImprovedHordesMod));
        }

        public void InitMod(Mod _modInstance)
        {
            Instance = this;
            Instance.modInstance = _modInstance;

            XPathPatcher.LoadAndPatchXMLFile(_modInstance, "Config/ImprovedHordes", "hordes.xml", xmlFile => HordesFromXml.LoadHordes(xmlFile));
            XPathPatcher.LoadAndPatchXMLFile(_modInstance, "Config/ImprovedHordes", "settings.xml", xmlFile => this.settingLoader = new ImprovedHordesSettingLoader(this.loggerFactory, xmlFile));

            this.settingLoader.RegisterTypeParser<int>(new ImprovedHordesSettingTypeParserInt());
            this.settingLoader.RegisterTypeParser<float>(new ImprovedHordesSettingTypeParserFloat());

            ModEvents.GameStartDone.RegisterHandler(GameStartDone);
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

        private static int GetWorldSize(World world)
        {
            if (!world.GetWorldExtent(out Vector3i minSize, out Vector3i maxSize))
            {
                throw new InvalidOperationException("Could not determine world size.");
            }

            return maxSize.x - minSize.x;
        }

        private void Patch(bool patch)
        {
            if(patch)
            {
                harmony.PatchAll();

                ModEvents.GameUpdate.RegisterHandler(GameUpdate);
                ModEvents.GameShutdown.RegisterHandler(GameShutdown);

#if EXPERIMENTAL
            new IHExperimentalManager(this.modInstance);
#endif
            }
            else
            {
                harmony.UnpatchSelf();

                ModEvents.GameUpdate.UnregisterHandler(GameUpdate);
                ModEvents.GameShutdown.UnregisterHandler(GameShutdown);
            }
        }

        private static string GetDataFile()
        {
            return $"{GameIO.GetSaveGameDir()}/ImprovedHordes.bin";
        }

        private void InitializeCore(World world)
        {
            if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                return;

            // Patch patches / register game event handlers.
            this.Patch(true);

            int worldSize = GetWorldSize(world);

            IRandomFactory<IWorldRandom> randomFactory = new ImprovedHordesWorldRandomFactory(worldSize, world);

            core = new ImprovedHordesCore(worldSize, this.loggerFactory, randomFactory, new ImprovedHordesEntitySpawner());
            this.poiScanner = new WorldPOIScanner(this.loggerFactory);
            this.dataParserRegistry = new ImprovedHordesDataParserRegistry(randomFactory, this.poiScanner, core.GetWorldEventReporter());

            core.GetWorldHordePopulator().RegisterPopulator(new WorldZoneWanderingEnemyHordePopulator(this.poiScanner));
            core.GetWorldHordePopulator().RegisterPopulator(new WorldZoneScreamerHordePopulator(this.poiScanner, core.GetWorldEventReporter()));

            core.GetWorldHordePopulator().RegisterPopulator(new WorldWildernessWanderingEnemyHordePopulator(core.GetWorldSize(), this.poiScanner, new HordeSpawnData(15)));
            core.GetWorldHordePopulator().RegisterPopulator(new WorldWildernessWanderingAnimalHordePopulator(core.GetWorldSize(), this.poiScanner, new HordeSpawnData(15)));
            core.GetWorldHordePopulator().RegisterPopulator(new WorldWildernessWanderingAnimalEnemyHordePopulator(core.GetWorldSize(), this.poiScanner, new HordeSpawnData(15)));

            this.settingLoader.LoadSettings();
            
            if(this.TryLoadData())
                this.logger.Info("Loaded data.");
        }

        private bool TryLoadData()
        {
            if (!File.Exists(GetDataFile()))
                return false;

            try
            {
                using (Stream stream = File.Open(GetDataFile(), FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        IDataLoader dataLoader = new ImprovedHordesDataLoader(this.loggerFactory, this.dataParserRegistry, reader);
                        core.Load(dataLoader);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                this.logger.Error($"Failed to load data from {GetDataFile()}.");
                this.logger.Exception(e);
            }

            return false;
        }

        private bool TrySaveData()
        {
            try
            {
                using(Stream stream = File.Open(GetDataFile(), FileMode.Create))
                {
                    using(BinaryWriter writer = new BinaryWriter(stream))
                    {
                        IDataSaver saver = new ImprovedHordesDataSaver(this.dataParserRegistry, writer);
                        core.Save(saver);
                    }
                }

                return true;
            }
            catch(Exception e)
            {
                this.logger.Warn("Failed to save data. " + e.Message + "\n " + e.StackTrace);
                this.logger.Exception(e);
            }

            return false;
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

            if (Instance.TrySaveData())
                Instance.logger.Info("Saved data.");

            Instance.core.Shutdown();
            Instance.core = null;

            // Unpatch all patches / unregister all game event handlers.
            Instance.Patch(false);
        }

        [HarmonyPatch(typeof(World))]
        [HarmonyPatch("Cleanup")]
        private sealed class World_Cleanup_Patch
        {
            private static void Prefix() // Clean up on client world exit
            {
                GameShutdown();
            }
        }

        [HarmonyPatch(typeof(World))]
        [HarmonyPatch(nameof(World.Save))]
        class World_Save_Patch
        {
            private static void Prefix()
            {
                if (Instance.TrySaveData())
                    Instance.logger.Info("Saved data.");
            }
        }
    }
}
