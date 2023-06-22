using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde
{
    public sealed class PlayerHordeGroup
    {
        private List<EntityPlayer> players = new List<EntityPlayer>();
        private Dictionary<string, int> biomes = new Dictionary<string, int>();

        public void AddPlayer(EntityPlayer player, int gamestage, string biome)
        {
            this.players.Add(player);

            if (biome != null)
            {
                if (!this.biomes.ContainsKey(biome))
                    this.biomes.Add(biome, 1);
                else
                    this.biomes[biome]++;
            }
        }

        public List<Vector3> GetLocations()
        {
            List<Vector3> locations = new List<Vector3>();

            foreach(var player in this.players) 
            {
                locations.Add(player.position);
            }

            return locations;
        }

        public int GetGamestage()
        {
            float gamestage = 0;

            float diminishingReturns = GameStageDefinition.DiminishingReturns;
            float difficultyBonus = GameStageDefinition.DifficultyBonus;

            foreach(var player in this.players)
            {
                gamestage += player.gameStage * difficultyBonus;
                difficultyBonus *= diminishingReturns;
            }

            return Mathf.RoundToInt(gamestage);
        }

        public int GetCount()
        {
            return this.players.Count;
        }

        public string GetBiome()
        {
            if (biomes.Count == 0)
                return "pine_forest";

            return biomes.Aggregate((e1, e2) => e1.Value > e2.Value ? e1 : e2).Key;
        }

        public override string ToString()
        {
            return $"[gamestage={this.GetGamestage()}, biome={this.GetBiome()}]";
        }
    }
}
