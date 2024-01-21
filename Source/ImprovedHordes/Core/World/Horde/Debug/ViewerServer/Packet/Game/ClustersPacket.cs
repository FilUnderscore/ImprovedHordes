#if DEBUG
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet.Game
{
    public sealed class ClustersPacket : Packet
    {
        private readonly Dictionary<Type, List<ClusterSnapshot>> clusters;

        public ClustersPacket(Dictionary<Type, List<ClusterSnapshot>> clusters) : base(Packets.CLUSTERS)
        {
            this.clusters = clusters;
        }

        protected override void OnSend(PacketBinaryWriter writer)
        {
            writer.Write(this.clusters.Keys.ToList(), clusterType =>
            {
                writer.Write(clusterType.Name);

                writer.WriteStruct(this.clusters[clusterType], cluster =>
                {
                    writer.Write(cluster.location);
                    writer.Write(cluster.density);
                });
            });
        }
    }
}
#endif