using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.World.Horde.Spawn;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class HordeSpawnParamsParser : IDataParser<HordeSpawnParams>
    {
        public HordeSpawnParams Load(IDataLoader loader, BinaryReader reader)
        {
            return new HordeSpawnParams(reader.ReadInt32());
        }

        public void Save(IDataSaver saver, BinaryWriter writer, HordeSpawnParams obj)
        {
            writer.Write(obj.SpreadDistanceLimit);
        }
    }
}
