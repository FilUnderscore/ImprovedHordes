using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.World.Horde.Spawn;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class HordeSpawnDataParser : IDataParser<HordeSpawnData>
    {
        public HordeSpawnData Load(IDataLoader loader, BinaryReader reader)
        {
            return new HordeSpawnData(reader.ReadInt32());
        }

        public void Save(IDataSaver saver, BinaryWriter writer, HordeSpawnData obj)
        {
            writer.Write(obj.SpreadDistanceLimit);
        }
    }
}
