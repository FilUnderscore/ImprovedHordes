using System;
using System.Collections.Generic;
using System.IO;

using static ImprovedHordes.Utils.Logger;

using ImprovedHordes.Horde;
using ImprovedHordes.Horde.AI;

using ImprovedHordes.Horde.Data;

using ImprovedHordes.Horde.Wandering;
using ImprovedHordes.Horde.Scout;
using ImprovedHordes.Horde.Heat;

using HarmonyLib;

namespace ImprovedHordes
{
    public class ImprovedHordesManager : IManager
    {
        private string DataFile;
        private readonly string XmlFilesDir;
        private readonly string ModPath;

        public World World;
        public GameRandom Random;

        private static ImprovedHordesManager instance;
        public static ImprovedHordesManager Instance
        {
            get
            {
                if (instance == null)
                    throw new NullReferenceException("Tried to access ImprovedHordesManager while still uninitialized.");

                return instance;
            }
        }

        public HordeManager HordeManager;
        public HordeAIManager AIManager;
        public WanderingHordeManager WanderingHorde;
        public ScoutManager ScoutManager;
        public Settings Settings;

        public HordePlayerManager PlayerManager;
        public HordeAreaHeatTracker HeatTracker;
        public HordeHeatPatrolManager HeatPatrolManager;

        public ImprovedHordesManager(Mod mod)
        {
            if (instance != null)
                throw new InvalidOperationException("ImprovedHordesManager instance has already been created on mod initialization.");

            instance = this;

            HordeManager = new HordeManager(this);
            AIManager = new HordeAIManager();
            WanderingHorde = new WanderingHordeManager(this);
            ScoutManager = new ScoutManager(this);
            PlayerManager = new HordePlayerManager(this);
            HeatTracker = new HordeAreaHeatTracker(this);
            HeatPatrolManager = new HordeHeatPatrolManager(this);

            ModPath = mod.Path;
            XmlFilesDir = string.Format("{0}/Config/ImprovedHordes", mod.Path);

            this.LoadXml();
        }

        public void Init()
        {
            World = GameManager.Instance.World;
            Random = GameRandomManager.Instance.CreateGameRandom(Guid.NewGuid().GetHashCode());

            this.WanderingHorde.schedule.SetGameVariables();
            this.HeatTracker.Init();

            // Reload data file location.
            DataFile = string.Format("{0}/ImprovedHordes.bin", GameIO.GetSaveGameDir());

            this.Load();
        }

        public void LoadSettings()
        {
            Log("Loading settings.");

            this.Settings = new Settings(new XmlFile(XmlFilesDir, "settings.xml"));
            this.WanderingHorde.ReadSettings(this.Settings.GetSettings("wandering_horde"));
            this.ScoutManager.ReadSettings(this.Settings.GetSettings("scout_horde"));

            HordeSpawner.ReadSettings(this.Settings);
            HordeGenerator.ReadSettings(this.Settings.GetSettings("horde_generator"));

            Log("Loaded settings.");
        }

        public void LoadXml()
        {
            Log("Loading Xml Configs in {0}", XmlFilesDir);

            this.LoadSettings();

            XPath.XPathPatcher.LoadAndPatchXMLFile(this.ModPath, "Config/ImprovedHordes", "hordes.xml", xmlFile => HordesFromXml.LoadHordes(xmlFile));

            Log("Loaded all Xml configs.");
        }

        public void Save()
        {
            try
            {
                using (Stream stream = File.Open(DataFile, FileMode.Create))
                {
                    BinaryWriter writer = new BinaryWriter(stream);

                    this.WanderingHorde.Save(writer);

                    Log("Saved horde data.");
                }
            }
            catch (Exception )
            {
                Warning("Failed to save Improved Hordes data, next startup will load default.");
            }
        }

        public void Load()
        {
            try
            {
                using(Stream stream = File.Open(DataFile, FileMode.Open))
                {
                    BinaryReader reader = new BinaryReader(stream);

                    this.WanderingHorde.Load(reader);

                    Log("Loaded horde data.");
                }
            }
            catch(Exception )
            {

            }
        }

        public void RemovePlayer(int playerId)
        {
            PlayerManager.RemovePlayer(playerId);
        }

        public void Update()
        {
            if (!this.Initialized()) // If world is null, the manager has not been initialized yet.
                return;

            this.AIManager.Update();
            this.WanderingHorde.Update();
            this.ScoutManager.Update();
            this.HeatPatrolManager.Update();
        }

        public void Tick(ulong time)
        {
            this.PlayerManager.Tick(time);
            this.HeatTracker.Tick(time);
        }

        public void EntityKilled(Entity killed, Entity killer)
        {
            this.AIManager.EntityKilled(killed, killer);
        }

        public void Shutdown()
        {
            Log("Cleaning up.");

            this.HordeManager.Shutdown();
            this.AIManager.Shutdown();
            this.WanderingHorde.Shutdown();
            this.ScoutManager.Shutdown();
            this.PlayerManager.Shutdown();
            this.HeatTracker.Shutdown();
            this.HeatPatrolManager.Shutdown();
        }

        public bool Initialized()
        {
            return this.World != null;
        }

        class HarmonyPatches
        {
            [HarmonyPatch(typeof(World))]
            [HarmonyPatch("SetTime")]
            class WorldSetTimeHook
            {
                static void Postfix(ulong _time)
                {
                    if (!ImprovedHordesMod.IsHost())
                        return;

                    Instance.Tick(_time);
                }
            }
        }
    }
}
