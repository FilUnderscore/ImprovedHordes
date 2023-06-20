using ImprovedHordes.Core.Abstractions.Data;
using System.IO;
using UnityEngine;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class Vector3DataParser : IDataParser<Vector3>
    {
        public Vector3 Load(IDataLoader loader, BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public void Save(IDataSaver saver, BinaryWriter writer, Vector3 obj)
        {
            writer.Write(obj.x);
            writer.Write(obj.y);
            writer.Write(obj.z);
        }
    }
}
