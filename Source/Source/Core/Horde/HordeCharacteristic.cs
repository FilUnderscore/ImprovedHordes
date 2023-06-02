﻿using System;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public interface IHordeCharacteristic
    {
        void Merge(IHordeCharacteristic other);
    }

    public abstract class HordeCharacteristic<T> : IHordeCharacteristic where T : HordeCharacteristic<T>
    {
        public void Merge(IHordeCharacteristic other)
        {
            if (!(other is T otherCharacteristic))
                throw new InvalidOperationException("Cannot merge two unrelated horde characteristics.");

            Merge(otherCharacteristic);
        }

        public abstract void Merge(T other);
    }
}
