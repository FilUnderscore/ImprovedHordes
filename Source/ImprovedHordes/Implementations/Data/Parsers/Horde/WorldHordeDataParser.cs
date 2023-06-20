using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Data;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class WorldHordeDataParser : IDataParser<WorldHorde>
    {
        private readonly IRandomFactory<IWorldRandom> randomFactory;

        public WorldHordeDataParser(IRandomFactory<IWorldRandom> randomFactory)
        {
            this.randomFactory = randomFactory;
        }

        public WorldHorde Load(IDataLoader loader, BinaryReader reader)
        {
            return new WorldHorde(loader.Load<WorldHordeData>(), this.randomFactory);
        }

        public void Save(IDataSaver saver, BinaryWriter writer, WorldHorde obj)
        {
            saver.Save<WorldHordeData>(obj.GetData());
        }
    }
}
