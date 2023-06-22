using ImprovedHordes.Core.Abstractions.Data;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class ULongDataParser : IDataParser<ulong>
    {
        public ulong Load(IDataLoader loader, BinaryReader reader)
        {
            return reader.ReadUInt64();
        }

        public void Save(IDataSaver saver, BinaryWriter writer, ulong obj)
        {
            writer.Write(obj);
        }
    }
}
