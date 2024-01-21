#if DEBUG
using ImprovedHordes.POI;
using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet.Login
{
    public sealed class ZonesPacket : Packet
    {
        private readonly List<WorldPOIScanner.POIZone> zones;

        public ZonesPacket(List<WorldPOIScanner.POIZone> zones) : base(Packets.ZONES)
        {
            this.zones = zones;
        }

        protected override void OnSend(PacketBinaryWriter writer)
        {
            writer.Write(this.zones, zone =>
            {
                writer.Write(new Vector2i((int)zone.GetBounds().min.x, (int)zone.GetBounds().min.z));
                writer.Write(new Vector2i((int)zone.GetBounds().size.x, (int)zone.GetBounds().size.z));

                writer.Write(zone.GetDensity());
                writer.Write(zone.GetCount());
                writer.Write(0.0f);
                writer.Write(0.0f);
            });
        }
    }
}
#endif