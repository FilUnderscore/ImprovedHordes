namespace ImprovedHordes.Source.Core.Threading
{
    public sealed class LockedObjectData<T>
    {
        public T value;
        public readonly object lockObject;

        public bool reading;
        public readonly object readingLockObject;

        public LockedObjectData(T value)
        {
            this.value = value;
            this.lockObject = new object();

            this.reading = false;
            this.readingLockObject = new object();
        }
    }
}
