#if DEBUG
namespace ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet
{
    public sealed class Packets
    {
        public static readonly short INIT = 0;

        // one time packets sent on login
        public static readonly short BIOMES = 1;
        public static readonly short ZONES = 2;
        public static readonly short HEAT = 3;

        // continuous packets sent
        public static readonly short PLAYERS = 4;
        public static readonly short CLUSTERS = 5;
        public static readonly short EVENT = 6;
    }
}
#endif