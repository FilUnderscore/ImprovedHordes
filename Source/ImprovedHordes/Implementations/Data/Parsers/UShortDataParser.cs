using ImprovedHordes.Core.Abstractions.Data;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class UShortDataParser : IDataParser<ushort>
    {
        public ushort Load(IDataLoader loader, BinaryReader reader)
        {
            return reader.ReadUInt16();
        }

        public void Save(IDataSaver saver, BinaryWriter writer, ushort obj)
        {
            writer.Write(obj);
        }
    }
}
