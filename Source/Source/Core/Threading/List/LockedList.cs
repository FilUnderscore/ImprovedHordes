using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Threading
{
    public sealed class LockedList<T> : LockedObject<List<T>, LockedListReader<T>, LockedListWriter<T>>
    {
        public LockedList() : base(new List<T>())
        {
        }
    }
}