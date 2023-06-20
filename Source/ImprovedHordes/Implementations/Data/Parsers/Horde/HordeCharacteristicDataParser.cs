using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.World.Horde.Characteristics;
using System;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class HordeCharacteristicDataParser : IDataParser<IHordeCharacteristic>
    {
        public IHordeCharacteristic Load(IDataLoader loader, BinaryReader reader)
        {
            Type type = loader.Load<Type>();

            return null;
        }

        public void Save(IDataSaver saver, BinaryWriter writer, IHordeCharacteristic obj)
        {

        }
    }
}
