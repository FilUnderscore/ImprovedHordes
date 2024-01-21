#if DEBUG
namespace ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet.Login
{
    public sealed class BiomesPacket : Packet
    {
        private readonly int biomesTexWidth, biomesTexHeight;
        private readonly byte[] biomesTexData;

        public BiomesPacket(int biomesTexWidth, int biomesTexHeight, byte[] biomesTexData) : base(Packets.BIOMES)
        {
            this.biomesTexWidth = biomesTexWidth;
            this.biomesTexHeight = biomesTexHeight;
            this.biomesTexData = biomesTexData;
        }

        protected override void OnSend(PacketBinaryWriter writer)
        {
            writer.Write(this.biomesTexWidth);
            writer.Write(this.biomesTexHeight);

            writer.Write(this.biomesTexData);
        }
    }
}
#endif