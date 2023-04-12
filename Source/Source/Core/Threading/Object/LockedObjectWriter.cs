using System;
using System.Threading;

namespace ImprovedHordes.Source.Core.Threading
{
    public class LockedObjectWriter<T> : IDisposable
    {
        private readonly LockedObjectData<T> data;
        private bool disposed = false;

        public LockedObjectWriter(LockedObjectData<T> data)
        {
            this.data = data;
        }

        public bool IsWriting()
        {
            return this.data != null;
        }

        public T Get()
        {
            this.ThrowIfNotWriting();
            return this.data.value;
        }

        public void Set(T value)
        {
            this.ThrowIfNotWriting();
            this.data.value = value;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            if(IsWriting())
                Monitor.Exit(this.data.lockObject);

            disposed = true;
        }

        protected void ThrowIfNotWriting()
        {
            if (!IsWriting())
                throw new InvalidOperationException($"Cannot write {typeof(T).FullName} while object lock has not been acquired. Check using LockedObjectWriter.IsWriting() before using.");
        }
    }
}
