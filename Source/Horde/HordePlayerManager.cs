using System.Collections.Generic;

namespace ImprovedHordes.Horde
{
    public sealed class HordePlayerManager : IManager
    {
        public readonly Dictionary<int, HordePlayer> players = new Dictionary<int, HordePlayer>();
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
        }

        public void Shutdown()
        {
            players.Clear();
        }
    }
}