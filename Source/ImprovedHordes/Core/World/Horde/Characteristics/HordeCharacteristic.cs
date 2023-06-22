using ImprovedHordes.Core.Abstractions.Data;
using System;

namespace ImprovedHordes.Core.World.Horde.Characteristics
{
    public interface IHordeCharacteristic : IData
    {
        void Merge(IHordeCharacteristic other);
    }

    public abstract class HordeCharacteristic<T> : IHordeCharacteristic where T : HordeCharacteristic<T>
    {
        public abstract IData Load(IDataLoader loader);

        public void Merge(IHordeCharacteristic other)
        {
            if (!(other is T otherCharacteristic))
                throw new InvalidOperationException("Cannot merge two unrelated horde characteristics.");

            Merge(otherCharacteristic);
        }

        public abstract void Merge(T other);
        public abstract void Save(IDataSaver saver);
    }
}
