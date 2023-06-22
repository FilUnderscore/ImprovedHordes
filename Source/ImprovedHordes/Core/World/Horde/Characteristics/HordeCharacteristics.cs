using ImprovedHordes.Core.Abstractions.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImprovedHordes.Core.World.Horde.Characteristics
{
    public sealed class HordeCharacteristics : IData
    {
        private readonly Dictionary<Type, IHordeCharacteristic> characteristics = new Dictionary<Type, IHordeCharacteristic>();

        public HordeCharacteristics() { }

        public HordeCharacteristics(params IHordeCharacteristic[] characteristics) 
        {
            foreach(var characteristic in characteristics) 
            {
                this.characteristics.Add(characteristic.GetType(), characteristic);
            }
        }

        public void RegisterCharacteristic<T>(HordeCharacteristic<T> characteristic) where T : HordeCharacteristic<T>
        {
            characteristics.Add(typeof(T), characteristic);
        }

        public T GetCharacteristic<T>() where T : HordeCharacteristic<T>
        {
            if (!HasCharacteristic<T>())
                throw new InvalidOperationException("Horde characteristic " + typeof(T).Name + " could not be found.");

            return (T)characteristics[typeof(T)];
        }

        public bool HasCharacteristic<T>() where T : HordeCharacteristic<T>
        {
            return characteristics.ContainsKey(typeof(T));
        }

        public void Merge(HordeCharacteristics other)
        {
            foreach(var characteristicKey in other.characteristics.Keys)
            {
                if (!this.characteristics.ContainsKey(characteristicKey))
                {
                    this.characteristics.Add(characteristicKey, other.characteristics[characteristicKey]);
                }
                else
                {
                    var characteristic = characteristics[characteristicKey];
                    var otherCharacteristic = other.characteristics[characteristicKey];

                    characteristic.Merge(otherCharacteristic);
                }
            }
        }

        public IData Load(IDataLoader loader)
        {
            List<IHordeCharacteristic> characteristics = loader.Load<List<IHordeCharacteristic>>();

            foreach(var characteristic in characteristics)
            {
                this.characteristics.Add(characteristic.GetType(), characteristic);
            }

            return this;
        }

        public void Save(IDataSaver saver)
        {
            saver.Save<List<IHordeCharacteristic>>(this.characteristics.Values.ToList());
        }
    }
}
