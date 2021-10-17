using System;
using System.Collections.Generic;
using System.IO;

using static ImprovedHordes.Utils.Logger;

using ImprovedHordes.Horde.AI;

using ImprovedHordes.Horde.Wandering;
using ImprovedHordes.Horde.Scout;

namespace ImprovedHordes.Horde
{
    public class HordeManager
    {
        public static readonly string DataFile = string.Format("{0}/ImprovedHordes.bin", GameUtils.GetSaveGameDir());
        public static string XmlFilesDir;

        public World World;
        public List<int> Players = new List<int>();
        public GameRandom Random;

        private static HordeManager instance;
        public static HordeManager Instance
        {
            get
            {
                if (instance == null)
                    throw new NullReferenceException("Tried to access HordeManager while still uninitialized.");

                return instance;
            }
        }

        public HordeAIManager AIManager;
        public WanderingHorde WanderingHorde;
        public ScoutManager ScoutManager;
        
        public HordeManager()
        {
            instance = this;

            World = GameManager.Instance.World;
            Random = GameRandomManager.Instance.CreateGameRandom(Guid.NewGuid().GetHashCode());

            AIManager = new HordeAIManager();
            WanderingHorde = new WanderingHorde(this);
            ScoutManager = new ScoutManager(this);

            XmlFilesDir = string.Format("{0}/Config/ImprovedHordes", ModManager.GetMod("ImprovedHordes").Path);
            this.LoadXml();
        }

        public void LoadXml()
        {
            Log("Loading Xml Configs in {0}", XmlFilesDir);

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
            this.AIManager.Update();
            this.WanderingHorde.Update();
        }
    }
}
