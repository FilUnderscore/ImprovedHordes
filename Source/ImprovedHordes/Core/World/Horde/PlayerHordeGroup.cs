using ImprovedHordes.Core.Abstractions.Settings;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde
{
    public readonly struct PlayerHordeGroup
    {
        private static readonly Setting<int> MAX_HORDES_SPAWNED_PER_PLAYER_GROUP = new Setting<int>("max_hordes_spawned_per_player_group", 3);

        private readonly List<PlayerSnapshot> players;
        private readonly PlayerHordeTracker tracker;

        public PlayerHordeGroup(PlayerSnapshot player)
        {
            this.players = new List<PlayerSnapshot>();
            this.tracker = MAX_HORDES_SPAWNED_PER_PLAYER_GROUP.Value > -1 ? player.tracker : null;

            this.AddPlayer(player);
        }

        public void AddPlayer(PlayerSnapshot player)
        {
            this.players.Add(player);
        }

        public void AddActiveHorde(WorldHorde horde)
        {
            if (this.tracker == null)
                return;

            this.tracker.ActiveHordes.Add(horde);
        }

        public void RemoveActiveHorde(WorldHorde horde)
        {
            if (this.tracker == null || !this.tracker.ActiveHordes.Contains(horde))
                return;

            this.tracker.ActiveHordes.Remove(horde);
        }

        public bool IsPlayerGroupExceedingHordeLimit(WorldHorde horde)
        {
            if(this.tracker != null && !this.tracker.ActiveHordes.Contains(horde) && this.tracker.ActiveHordes.Count >= MAX_HORDES_SPAWNED_PER_PLAYER_GROUP.Value)
            {
                // Try purge dead hordes.
                int removed = this.tracker.ActiveHordes.RemoveAll(activeHorde => activeHorde == null || activeHorde.IsDead());
                return this.tracker.ActiveHordes.Count - removed >= MAX_HORDES_SPAWNED_PER_PLAYER_GROUP.Value;
            }

            return false;
        }

        public List<PlayerSnapshot> GetPlayers()
        {
            return this.players;
        }

        public int GetGamestage()
        {
            float gamestage = 0;

            float startingWeight = GameStageDefinition.StartingWeight;
            float diminishingReturns = GameStageDefinition.DiminishingReturns;

            this.players.Sort((a, b) =>
            {
                if (a.player.gameStage > b.player.gameStage)
                    return -1;
                else if (a.player.gameStage < b.player.gameStage)
                    return 1;
                else
                    return 0;
            });

            foreach (var player in this.players)
            {
                gamestage += player.player.gameStage * startingWeight;
                startingWeight *= diminishingReturns;
            }

            return Mathf.FloorToInt(gamestage);
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
            return $"[gamestage={this.GetGamestage()}, biome={this.GetBiome()}, activeHordes={this.tracker.ActiveHordes.Count}]";
        }

        private Vector2 ToXZ(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public PlayerSnapshot GetPlayerClosestTo(Vector2 location, out float distance)
        {
            PlayerSnapshot closest = this.players[0];
            float closestDistance = Vector2.Distance(ToXZ(closest.location), location);

            for (int i = 1; i < this.players.Count; i++)
            {
                PlayerSnapshot player = this.players[i];
                float playerDistance = Vector2.Distance(location, ToXZ(player.location));

                if (playerDistance < closestDistance)
                {
                    closest = player;
                    closestDistance = playerDistance;
                }
            }

            distance = closestDistance;
            return closest;
        }

        public PlayerSnapshot GetPlayerClosestTo(Vector3 location, out float distance)
        {
            PlayerSnapshot closest = this.players[0];
            float closestDistance = Vector3.Distance(closest.location, location);

            for (int i = 1; i < this.players.Count; i++)
            {
                PlayerSnapshot player = this.players[i];
                float playerDistance = Vector3.Distance(location, player.location);

                if(playerDistance < closestDistance)
                {
                    closest = player;
                    closestDistance = playerDistance;
                }
            }

            distance = closestDistance;
            return closest;
        }
    }
}
