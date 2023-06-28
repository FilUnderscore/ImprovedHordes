using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ImprovedHordes.Core.World.Horde.WorldHordeTracker;

namespace ImprovedHordes.Core.World.Horde
{
    public readonly struct PlayerHordeGroup
    {
        private readonly List<PlayerSnapshot> players;
        
        public PlayerHordeGroup(PlayerSnapshot player)
        {
            this.players = new List<PlayerSnapshot>();
            this.AddPlayer(player);
        }

        public void AddPlayer(PlayerSnapshot player)
        {
            this.players.Add(player);
        }

        public List<PlayerSnapshot> GetPlayers()
        {
            return this.players;
        }

        public List<Vector3> GetLocations()
        {
            List<Vector3> locations = new List<Vector3>();

            foreach(var player in this.players) 
            {
                locations.Add(player.location);
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
                gamestage += player.player.gameStage * difficultyBonus;
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
            Dictionary<string, int> biomes = new Dictionary<string, int>();

            foreach(var player in this.players)
            {
                if (player.player.biomeStandingOn == null)
                    continue;

                string biomeName = player.player.biomeStandingOn.m_sBiomeName;

                if(biomes.TryGetValue(biomeName, out var count))
                {
                    biomes[biomeName]++;
                }
                else
                    biomes.Add(player.player.biomeStandingOn.m_sBiomeName, 1);
            }

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
