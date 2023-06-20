using ImprovedHordes.Core.Abstractions.Data;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class FloatDataParser : IDataParser<float>
    {
        public float Load(IDataLoader loader, BinaryReader reader)
        {
            return reader.ReadSingle();
        }

        public void Save(IDataSaver saver, BinaryWriter writer, float obj)
        {
            writer.Write(obj);
        }
    }
}
