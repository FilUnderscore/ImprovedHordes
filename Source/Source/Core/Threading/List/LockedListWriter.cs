using System.Collections;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Threading
{
    public sealed class LockedListWriter<T> : LockedObjectWriter<List<T>>
    {
        public LockedListWriter(LockedObjectData<List<T>> data) : base(data)
        {
        }

        public void Add(T obj)
        {
            this.Get().Add(obj);
        }

        public void AddRange(IEnumerable<T> enumerable)
        {
            this.Get().AddRange(enumerable);
        }

        public void Remove(T obj)
        {
            this.Get().Remove(obj);
        }

        public int GetCount()
        {
            return this.Get().Count;
        }

        public T Get(int index)
        {
            return this.Get()[index];
        }

        public bool Contains(T obj)
        {
            return this.Get().Contains(obj);
        }

        public void Clear()
        {
            this.Get().Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.Get().GetEnumerator();
        }
    }
}
