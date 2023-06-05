using System;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class HordeCharacteristics
    {
        private readonly Dictionary<Type, IHordeCharacteristic> characteristics = new Dictionary<Type, IHordeCharacteristic>();

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
    }
}
