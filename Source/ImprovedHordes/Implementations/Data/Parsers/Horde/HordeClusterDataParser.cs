using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.World.Horde.Cluster;
using ImprovedHordes.Core.World.Horde.Cluster.Data;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class HordeClusterDataParser : IDataParser<HordeCluster>
    {
        public HordeCluster Load(IDataLoader loader, BinaryReader reader)
        {
            return new HordeCluster(loader.Load<HordeClusterData>());
        }

        public void Save(IDataSaver saver, BinaryWriter writer, HordeCluster obj)
        {
            saver.Save<HordeClusterData>(obj.GetData());
        }
    }
}
