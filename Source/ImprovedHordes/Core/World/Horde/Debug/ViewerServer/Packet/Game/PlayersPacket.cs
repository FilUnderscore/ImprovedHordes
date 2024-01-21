#if DEBUG
using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet.Game
{
    public sealed class PlayersPacket : Packet
    {
        private readonly List<PlayerHordeGroup> playerGroups;

        public PlayersPacket(List<PlayerHordeGroup> playerGroups) : base(Packets.PLAYERS)
        {
            this.playerGroups = playerGroups;
        }

        protected override void OnSend(PacketBinaryWriter writer)
        {
            writer.WriteStruct(this.playerGroups, playerGroup =>
            {
                writer.WriteStruct(playerGroup.GetPlayers(), player =>
                {
                    writer.Write(player.location);
                    writer.Write(player.player.gameStage);
                    writer.Write(player.player.biomeStandingOn?.m_sBiomeName);
                });
            });
        }
    }
}
#endif