using System;
using System.Collections.Generic;
using System.IO;

using static ImprovedHordes.Utils.Logger;

using ImprovedHordes.Horde.AI;

using ImprovedHordes.Horde.Wandering;

namespace ImprovedHordes.Horde
{
    public class HordeManager
    {
        public static readonly string DataFile = string.Format("{0}/ImprovedHordes.bin", GameUtils.GetSaveGameDir());
        public static string XmlFilesDir;

        public World world;
        public List<int> players = new List<int>();
        public GameRandom random;

        public HordeAIManager aiManager;
        public WanderingHorde wanderingHorde;
        
        public HordeManager()
        {
            world = GameManager.Instance.World;
            random = GameRandomManager.Instance.CreateGameRandom(Guid.NewGuid().GetHashCode());

            aiManager = new HordeAIManager();
            wanderingHorde = new WanderingHorde(this);

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

                    this.wanderingHorde.Save(writer);

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

                    this.wanderingHorde.Load(reader);

                    Log("Loaded horde data.");
                }
            }
            catch(Exception )
            {

            }
        }

        public void AddPlayer(int playerId)
        {
            this.players.Add(playerId);
        }

        public void RemovePlayer(int playerId)
        {
            this.players.Remove(playerId);
        }

        public void Update()
        {
            this.aiManager.Update();
            this.wanderingHorde.Update();
        }

        public ulong GetWorldTime()
        {
            return world.GetWorldTime();
        }
    }
}
