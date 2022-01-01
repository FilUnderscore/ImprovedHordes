using System;
using System.Collections.Generic;
using System.IO;

using static ImprovedHordes.Utils.Logger;

using ImprovedHordes.Horde;
using ImprovedHordes.Horde.AI;

using ImprovedHordes.Horde.Data;

using ImprovedHordes.Horde.Wandering;
using ImprovedHordes.Horde.Scout;

namespace ImprovedHordes
{
    public class ImprovedHordesManager : IManager
    {
        private string DataFile;
        private readonly string XmlFilesDir;

        public World World;
        public List<int> Players = new List<int>();
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

        public HordeAIManager AIManager;
        public WanderingHordeManager WanderingHorde;
        public ScoutManager ScoutManager;
        public Settings Settings;

        public ImprovedHordesManager(Mod mod)
        {
            if (instance != null)
                throw new InvalidOperationException("ImprovedHordesManager instance has already been created on mod initialization.");

            instance = this;

            AIManager = new HordeAIManager();
            WanderingHorde = new WanderingHordeManager(this);
            ScoutManager = new ScoutManager(this);

            XmlFilesDir = string.Format("{0}/Config/ImprovedHordes", mod.Path);

            this.LoadXml();
        }

        public void Init()
        {
            World = GameManager.Instance.World;
            Random = GameRandomManager.Instance.CreateGameRandom(Guid.NewGuid().GetHashCode());

            this.WanderingHorde.schedule.SetGameVariables();

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

            Log("Loaded settings.");
        }

        public void LoadXml()
        {
            Log("Loading Xml Configs in {0}", XmlFilesDir);

            this.LoadSettings();

            HordesFromXml.LoadHordes(new XmlFile(XmlFilesDir, "hordes.xml"));

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

        public void AddPlayer(int playerId)
        {
            this.Players.Add(playerId);
        }

        public void RemovePlayer(int playerId)
        {
            this.Players.Remove(playerId);
        }

        public void Update()
        {
            if (!this.Initialized()) // If world is null, the manager has not been initialized yet.
                return;

            this.AIManager.Update();
            this.WanderingHorde.Update();
            this.ScoutManager.Update();
        }

        public void EntityKilled(Entity killed, Entity killer)
        {
            this.AIManager.EntityKilled(killed, killer);
        }

        public void Shutdown()
        {
            Log("Cleaning up.");

            this.AIManager.Shutdown();
            this.WanderingHorde.Shutdown();
            this.ScoutManager.Shutdown();
        }

        public bool Initialized()
        {
            return this.World != null;
        }
    }
}
