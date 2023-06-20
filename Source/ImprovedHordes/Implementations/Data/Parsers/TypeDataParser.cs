using ImprovedHordes.Core.Abstractions.Data;
using System;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class TypeDataParser : IDataParser<Type>
    {
        public Type Load(IDataLoader loader, BinaryReader reader)
        {
            return Type.GetType(reader.ReadString(), false, false);
        }

        public void Save(IDataSaver saver, BinaryWriter writer, Type obj)
        {
            writer.Write(obj.FullName);
        }
    }
}
