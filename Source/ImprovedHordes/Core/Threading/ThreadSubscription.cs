using System.Collections.Concurrent;

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
        private readonly ConcurrentQueue<T> history = new ConcurrentQueue<T>();
        private T previous;

        public bool TryGet(out T next)
        {
            if(history.IsEmpty)
            {
                next = previous;
                return previous != null;
            }

            return history.TryDequeue(out next);
        }

        public void Add(T item)
        {
            if (history.Count > 20)
                history.TryDequeue(out _);

            history.Enqueue(item);
            previous = item;
        }
    }
}
