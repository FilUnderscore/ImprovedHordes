using ImprovedHordes.Core.Abstractions.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            var copy = obj.ToList();
            writer.Write(copy.Count);

            foreach(var item in copy) 
            {
                saver.Save<T>(item);
            }
        }
    }
}
