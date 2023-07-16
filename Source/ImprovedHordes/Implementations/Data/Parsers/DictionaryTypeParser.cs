using ImprovedHordes.Core.Abstractions.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class DictionaryTypeParser<K, V> : IDataParser<Dictionary<K, V>>
    {
        public Dictionary<K, V> Load(IDataLoader loader, BinaryReader reader)
        {
            Dictionary<K, V> dictionary = new Dictionary<K, V>();

            int count = reader.ReadInt32();

            for(int i = 0; i < count; i++)
            {
                K key = loader.Load<K>();
                V value = loader.Load<V>();

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        public void Save(IDataSaver saver, BinaryWriter writer, Dictionary<K, V> obj)
        {
            var copy = obj.ToDictionary(k => k.Key, v => v.Value);
            writer.Write(copy.Count);

            foreach(var entry in copy)
            {
                saver.Save<K>(entry.Key);
                saver.Save<V>(entry.Value);
            }
        }
    }
}
