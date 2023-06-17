﻿using HarmonyLib;
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

namespace ImprovedHordes
{
    public sealed class ImprovedHordesMod : IModApi
    {
        private static ImprovedHordesMod Instance;

        private readonly Harmony harmony;
        private readonly ILoggerFactory loggerFactory;
        private ISettingLoader settingLoader;

        private ImprovedHordesCore core;
        private WorldPOIScanner poiScanner;

        public ImprovedHordesMod()
        {
            this.harmony = new Harmony("filunderscore.improvedhordes");
            this.loggerFactory = new ImprovedHordesLoggerFactory();
        }

        public void InitMod(Mod _modInstance)
        {
            Instance = this;

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
            new IHExperimentalManager(_modInstance);
#endif
            }
            else
            {
                harmony.UnpatchSelf();

                ModEvents.GameUpdate.UnregisterHandler(GameUpdate);
                ModEvents.GameShutdown.UnregisterHandler(GameShutdown);
            }
        }

        private void InitializeCore(World world)
        {
            if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                return;

            // Patch patches / register game event handlers.
            this.Patch(true);

            int worldSize = GetWorldSize(world);

            core = new ImprovedHordesCore(worldSize, this.loggerFactory, new ImprovedHordesWorldRandomFactory(worldSize, world), new ImprovedHordesEntitySpawner(), world);
            this.poiScanner = new WorldPOIScanner(this.loggerFactory);

            core.GetWorldHordePopulator().RegisterPopulator(new WorldZoneWanderingEnemyHordePopulator(this.poiScanner));
            core.GetWorldHordePopulator().RegisterPopulator(new WorldZoneScreamerHordePopulator(this.poiScanner, core.GetWorldEventReporter()));

            core.GetWorldHordePopulator().RegisterPopulator(new WorldWildernessWanderingEnemyHordePopulator(core.GetWorldSize(), this.poiScanner, new HordeSpawnData(15)));
            core.GetWorldHordePopulator().RegisterPopulator(new WorldWildernessWanderingAnimalHordePopulator(core.GetWorldSize(), this.poiScanner, new HordeSpawnData(15)));
            core.GetWorldHordePopulator().RegisterPopulator(new WorldWildernessWanderingAnimalEnemyHordePopulator(core.GetWorldSize(), this.poiScanner, new HordeSpawnData(15)));

            this.settingLoader.LoadSettings();
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

            // Unpatch all patches / unregister all game event handlers.
            Instance.Patch(false);
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
