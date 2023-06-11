using System.Collections.Generic;
using System.Linq;

namespace ImprovedHordes.Core.World.Horde
{
    public sealed class PlayerHordeGroup
    {
        private int gamestageSum;
        private Dictionary<string, int> biomes = new Dictionary<string, int>();

        private int count;

        public PlayerHordeGroup()
        {
            this.gamestageSum = 0;
            this.count = 0;
        }

        public void AddPlayer(int gamestage, string biome)
        {
            this.gamestageSum += gamestage;

            if (biome != null)
            {
                if (!this.biomes.ContainsKey(biome))
                    this.biomes.Add(biome, 1);
                else
                    this.biomes[biome]++;
            }

            this.count += 1;
        }

        public int GetGamestage()
        {
            if (this.count == 0)
                return 0;

            return this.gamestageSum / this.count;
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
