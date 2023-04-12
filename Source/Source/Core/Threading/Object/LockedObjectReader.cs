using System;
using System.Threading;

namespace ImprovedHordes.Source.Core.Threading
{
    public class LockedObjectReader<T> : IDisposable
    {
        protected readonly T value;
        protected readonly LockedObjectData<T> data;

        private bool disposed = false;

        public LockedObjectReader(LockedObjectData<T> data)
        {
            this.value = data != null ? data.value : default;
            this.data = data;
        }

        public T Get()
        {
            this.ThrowIfNotReading();
            return this.value;
        }

        public bool IsReading()
        {
            return this.data != null;
        }

        public virtual void Dispose()
        {
            if (disposed)
                return;

            if(IsReading())
            {
                if(Monitor.IsEntered(this.data.lockObject))
                {
                    Monitor.Enter(this.data.readingLockObject);
                    this.data.reading = false;
                    Monitor.Exit(this.data.readingLockObject);

                    Monitor.Exit(this.data.lockObject);
                }
            }

            disposed = true;
        }

        protected void ThrowIfNotReading()
        {
            if (!IsReading())
                throw new InvalidOperationException($"Cannot read {typeof(T).FullName} while object lock has not been acquired. Check using LockedObjectReader.IsReading() before using.");
        }
    }
}
