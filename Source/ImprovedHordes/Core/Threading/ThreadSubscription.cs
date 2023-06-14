using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ImprovedHordes.Core.Threading
{
    public sealed class ThreadSubscription<T>
    {
        private readonly ConcurrentBag<ThreadSubscriber<T>> subscriptions = new ConcurrentBag<ThreadSubscriber<T>>();
        private T recent;

        public ThreadSubscriber<T> Subscribe()
        {
            ThreadSubscriber<T> subscription = new ThreadSubscriber<T>();
            subscriptions.Add(subscription);

            // Add on first subscription the most recent copy.
            if(this.recent != null)
                subscription.Add(this.recent);

            return subscription;
        }

        public void Update(T recent)
        {
            this.recent = recent;

            foreach(var subscription in subscriptions)
            {
                subscription.Add(recent);
            }
        }
    }

    public sealed class ThreadSubscriber<T>
    {
        private ConcurrentQueue<T> history = new ConcurrentQueue<T>();
        
        public bool TryGet(out T next)
        {
            if(history.IsEmpty)
            {
                next = default(T);
                return false;
            }
            else
            {
                while (history.Count > 100)
                    history.TryDequeue(out _);
            }

            List<T> reverseList = history.ToList();
            reverseList.Reverse();

            next = reverseList[0];
            history.TryDequeue(out _);

            return true;
        }

        public void Add(T item)
        {
            history.Enqueue(item);
        }
    }
}
