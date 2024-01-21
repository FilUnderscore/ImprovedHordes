#if DEBUG
namespace ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet.Login
{
    public sealed class InitPacket : Packet
    {
        private readonly int worldSize;
        private readonly int viewDistance;

        public InitPacket(int worldSize, int viewDistance) : base(Packets.INIT)
        {
            this.worldSize = worldSize;
            this.viewDistance = viewDistance;
        }

        protected override void OnSend(PacketBinaryWriter writer)
        {
            writer.Write(this.worldSize);
            writer.Write(this.viewDistance);
        }
    }
}
#endif