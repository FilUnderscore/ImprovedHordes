using ImprovedHordes.Core.Abstractions.Data;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class Vector2iDataParser : IDataParser<Vector2i>
    {
        public Vector2i Load(IDataLoader loader, BinaryReader reader)
        {
            return new Vector2i(reader.ReadInt32(), reader.ReadInt32());
        }

        public void Save(IDataSaver saver, BinaryWriter writer, Vector2i obj)
        {
            writer.Write(obj.x);
            writer.Write(obj.y);
        }
    }
}
