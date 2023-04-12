using System.Collections;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Threading
{
    public sealed class LockedListReader<T> : LockedObjectReader<List<T>>
    {
        public LockedListReader(LockedObjectData<List<T>> data) : base(data)
        {
        }

        public int GetCount()
        {
            this.ThrowIfNotReading();
            return this.value.Count;
        }

        public T Get(int index)
        {
            this.ThrowIfNotReading();
            return this.value[index];
        }

        public bool Contains(T obj) 
        {
            this.ThrowIfNotReading();
            return this.value.Contains(obj);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.value.GetEnumerator();
        }
    }
}