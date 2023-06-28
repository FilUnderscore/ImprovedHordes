using ImprovedHordes.Core.Abstractions.Data;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class BiomeDefinitionDataParser : IDataParser<BiomeDefinition>
    {
        private readonly global::World world;

        public BiomeDefinitionDataParser(global::World world) 
        {
            this.world = world;
        }

        public BiomeDefinition Load(IDataLoader loader, BinaryReader reader)
        {
            return this.world.Biomes.GetBiome(reader.ReadByte());
        }

        public void Save(IDataSaver saver, BinaryWriter writer, BiomeDefinition obj)
        {
            writer.Write(obj.m_Id);
        }
    }
}
