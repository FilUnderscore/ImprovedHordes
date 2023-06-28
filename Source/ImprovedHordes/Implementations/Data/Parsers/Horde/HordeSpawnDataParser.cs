using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.World.Horde.Spawn;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers.Horde
{
    public sealed class HordeSpawnDataParser : IDataParser<HordeSpawnData>
    {
        public HordeSpawnData Load(IDataLoader loader, BinaryReader reader)
        {
            return new HordeSpawnData(loader.Load<HordeSpawnParams>(), loader.Load<BiomeDefinition>());
        }

        public void Save(IDataSaver saver, BinaryWriter writer, HordeSpawnData obj)
        {
            saver.Save<HordeSpawnParams>(obj.SpawnParams);
            saver.Save<BiomeDefinition>(obj.SpawnBiome);
        }
    }
}
