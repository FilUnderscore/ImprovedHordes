using System;
using System.Threading;

namespace ImprovedHordes.Source.Core.Threading
{
    public class LockedObject<T,Reader,Writer> where Reader: LockedObjectReader<T> 
                                                      where Writer: LockedObjectWriter<T>
    {
        private readonly LockedObjectData<T> data;

        public LockedObject(T value)
        {
            this.data = new LockedObjectData<T>(value);
        }

        private Reader CreateReader(bool reading)
        {
            return (Reader)Activator.CreateInstance(typeof(Reader), new object[] { reading ? this.data : null });
        }

        /// <summary>
        /// Non-blocking/blocking get, may be called in a loop which grants a chance at reading it.
        /// </summary>
        /// <param name="value">Value of object returned.</param>
        /// <returns>Whether reading the object was successful.</returns>
        public Reader Get(bool block)
        {
            bool result = block || Monitor.TryEnter(data.lockObject);

            if(block)
            {
                Monitor.Enter(data.lockObject);
            }

            Monitor.Enter(data.readingLockObject);
            data.reading |= result;

            Reader reader = CreateReader(data.reading);
            Monitor.Exit(data.readingLockObject);

            return reader;
        }

        private Writer CreateWriter(bool writing)
        {
            return (Writer)Activator.CreateInstance(typeof(Writer), new object[] { writing ? this.data : null });
        }

        public Writer Set(bool block)
        {
            Monitor.Enter(data.readingLockObject);
            bool result = block || Monitor.TryEnter(data.lockObject);

            if(block)
            {
                Monitor.Enter(data.lockObject);
            }

            if (result)
            {
                data.reading = false;
            }

            Monitor.Exit(data.readingLockObject);
            return CreateWriter(result);
        }
    }
}
