using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ImprovedHordes.Source.Core.Threading
{
    public sealed class LockedList<T>
    {
        private readonly List<T> list = new List<T>();
        private readonly object lockObject = new object();

        private bool reading = false;
        private readonly object readingLock = new object();

        public void Add(T item) 
        {
            this.list.Add(item);
        }

        public void Remove(T item)
        {
            this.list.Remove(item);
        }

        public bool Contains(T item)
        {
            return this.list.Contains(item);
        }

        public int GetCount()
        {
            return this.list.Count;
        }

        public List<T> GetList()
        {
            return list;
        }

        /*
        public void BlockRead()
        {
            this.notification.WaitOne();
            this.TryRead();
        }
        */

        public bool TryRead()
        {
            bool result = Monitor.TryEnter(this.lockObject);

            Monitor.Enter(this.readingLock);

            if (result)
            {
                this.reading = true;
            }

            result |= reading;
            Monitor.Exit(this.readingLock);

            return result;
        }

        public void EndRead()
        {
            if (Monitor.IsEntered(this.lockObject))
            {
                Monitor.Enter(this.readingLock);
                this.reading = false;
                Monitor.Exit(this.readingLock);

                Monitor.Exit(this.lockObject);
            }
        }

        public void StartWrite()
        {
            Monitor.Enter(this.lockObject);

            Monitor.Enter(this.readingLock);
            this.reading = false;
            Monitor.Exit(this.readingLock);
        }

        public void EndWrite()
        {
            Monitor.Exit(this.lockObject);
        }

        public IEnumerator GetEnumerator()
        {
            return this.list.GetEnumerator();
        }
    }
}