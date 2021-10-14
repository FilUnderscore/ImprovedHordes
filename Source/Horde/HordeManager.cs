using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using static ImprovedHordes.IHLog;

namespace ImprovedHordes.Horde
{
    //1000 is one hour
    class HordeManager
    {
        public static readonly string DataFile = string.Format("{0}/ImprovedHordes.bin", GameUtils.GetSaveGameDir());
        public static string XmlFilesDir;

        public World world;
        public List<int> players = new List<int>();
        public GameRandom random;

        public WanderingHordeManager wanderingHorde;
        
        public HordeManager()
        {
            world = GameManager.Instance.World;
            random = GameRandomManager.Instance.CreateGameRandom(Guid.NewGuid().GetHashCode());
            
            wanderingHorde = new WanderingHordeManager(this);

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

                    wanderingHorde.Save(writer);

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

                    wanderingHorde.Load(reader);

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
            wanderingHorde.Update();
        }

        public ulong GetWorldTime()
        {
            return world.GetWorldTime();
        }
    }
}
