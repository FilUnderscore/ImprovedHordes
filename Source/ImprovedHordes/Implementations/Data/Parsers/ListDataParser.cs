using ImprovedHordes.Core.Abstractions.Data;
using System.Collections.Generic;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class ListDataParser<T> : IDataParser<List<T>>
    {
        public List<T> Load(IDataLoader loader, BinaryReader reader)
        {
            List<T> list = new List<T>();
            int count = reader.ReadInt32();

            for(int i = 0; i < count; i++)
            {
                list.Add(loader.Load<T>());
            }

            return list;
        }

        public void Save(IDataSaver saver, BinaryWriter writer, List<T> obj)
        {
            writer.Write(obj.Count);

            foreach(var item in obj) 
            {
                saver.Save<T>(item);
            }
        }
    }
}
