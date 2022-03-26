using System.Collections.Generic;
using System.Linq;

namespace ImprovedHordes.Horde
{
    public sealed class HordePlayerManager : IManager
    {
        private readonly Dictionary<int, HordePlayer> players = new Dictionary<int, HordePlayer>();
        private readonly List<int> toRemove = new List<int>();
        private readonly ImprovedHordesManager manager;

        public HordePlayerManager(ImprovedHordesManager manager)
        {
            this.manager = manager;
        }

        public void Tick(ulong worldTime)
        {
            foreach (var player in manager.World.Players.list)
            {
                if (!players.ContainsKey(player.entityId))
                    players.Add(player.entityId, new HordePlayer(player));

                players[player.entityId].Tick(worldTime);
            }

            if(toRemove.Count > 0)
            {
                foreach(var player in toRemove)
                {
                    players.Remove(player);
                }

                toRemove.Clear();
            }
        }

        public void RemovePlayer(int playerId)
        {
            toRemove.Add(playerId);
        }

        public HordePlayer GetPlayer(int id)
        {
            if (!players.ContainsKey(id))
                return null;

            return players[id];
        }

        public int[] GetPlayers()
        {
            return players.Keys.ToArray();
        }

        public bool AnyPlayers()
        {
            return players.Count > 0;
        }

        public void Shutdown()
        {
            players.Clear();
            toRemove.Clear();
        }
    }
}