using System;
using System.IO;

using static ImprovedHordes.Utils.Logger;

using ImprovedHordes.Horde;
using ImprovedHordes.Horde.AI;

using ImprovedHordes.Horde.Data;

using ImprovedHordes.Horde.Wandering;
using ImprovedHordes.Horde.Scout;
using ImprovedHordes.Horde.Heat;

using HarmonyLib;

using CustomModManager.API;

namespace ImprovedHordes
{
    public class ImprovedHordesManager : IManager
    {
        private const ushort DATA_FILE_MAGIC = 0x4948;
        private const uint DATA_FILE_VERSION = 1;

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
            this.LoadSettings(mod);
        }

        public void Init()
        {
            World = GameManager.Instance.World;
            Random = GameRandomManager.Instance.CreateGameRandom(Guid.NewGuid().GetHashCode());

            this.WanderingHorde.schedule.SetGameVariables();
            
            // Reload data file location.
            DataFile = string.Format("{0}/ImprovedHordes.bin", GameIO.GetSaveGameDir());

            this.Load();
            this.HeatTracker.Init();
        }

        public void LoadSettings(Mod modInstance)
        {
            Log("Loading settings.");

            if (!ModManagerAPI.IsModManagerLoaded())
            {
                this.Settings = new Settings(new XmlFile(XmlFilesDir, "settings.xml"));
                this.WanderingHorde.ReadSettings(this.Settings.GetSettings("wandering_horde"));
                this.ScoutManager.ReadSettings(this.Settings.GetSettings("scout_horde"));

                HordeSpawner.ReadSettings(this.Settings);
                HordeGenerator.ReadSettings(this.Settings.GetSettings("horde_generator"));

                this.HeatTracker.ReadSettings(this.Settings.GetSettings("heat_tracker"));

                this.AIManager.ReadSettings(this.Settings.GetSettings("horde_ai"));
            }
            else
            {
                ModManagerAPI.ModSettings modSettings = ModManagerAPI.GetModSettings(modInstance);

                modSettings.CreateTab("hordeGeneralSettingsTab", "IHxuiHordeGeneralSettingsTab");

                HordeSpawner.HookSettings(modSettings);
                HordeGenerator.HookSettings(modSettings);

                this.WanderingHorde.HookSettings(modSettings);
                this.ScoutManager.HookSettings(modSettings);

                this.HeatTracker.HookSettings(modSettings);

                this.AIManager.HookSettings(modSettings);
            }

            Log("Loaded settings.");
        }

        public void LoadXml()
        {
            Log("Loading Xml Configs in {0}", XmlFilesDir);

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

                    writer.Write(DATA_FILE_MAGIC);
                    writer.Write(DATA_FILE_VERSION);

                    this.WanderingHorde.Save(writer);
                    this.HeatTracker.Save(writer);
                    this.HeatPatrolManager.Save(writer);

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
            if (!File.Exists(DataFile))
                return;

            try
            {
                using(Stream stream = File.Open(DataFile, FileMode.Open))
                {
                    BinaryReader reader = new BinaryReader(stream);

                    if(reader.ReadUInt16() != DATA_FILE_MAGIC || reader.ReadUInt32() < DATA_FILE_VERSION)
                    {
                        Log("Data file version has changed.");

                        return;
                    }

                    this.WanderingHorde.Load(reader);
                    this.HeatTracker.Load(reader);
                    this.HeatPatrolManager.Load(reader);

                    Log("Loaded horde data.");
                }
            }
            catch(Exception e)
            {
                Error("Failed to load: " + e.Message + " S: " + e.Source + " E: " + e.StackTrace);
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
            this.HordeManager.Update();
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
