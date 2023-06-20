using ImprovedHordes.Core.Abstractions.Data;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class UIntDataParser : IDataParser<uint>
    {
        public uint Load(IDataLoader loader, BinaryReader reader)
        {
            return reader.ReadUInt32();
        }

        public void Save(IDataSaver saver, BinaryWriter writer, uint obj)
        {
            writer.Write(obj);
        }
    }
}
