#if DEBUG
using System.IO;

namespace ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet
{
    public abstract class Packet
    {
        private readonly short id;

        public Packet(short id)
        {
            this.id = id;
        }

        public short GetId()
        {
            return this.id;
        }

        public void Send(BinaryWriter writer)
        {
            writer.Write(this.GetId());

            using(MemoryStream stream = new MemoryStream())
            {
                PacketBinaryWriter packetBinaryWriter = new PacketBinaryWriter(new BinaryWriter(stream));
                this.OnSend(packetBinaryWriter);

                writer.Write((int)stream.Length);
                writer.Write(stream.ToArray());
            }
        }

        protected abstract void OnSend(PacketBinaryWriter writer);
    }
}
#endif