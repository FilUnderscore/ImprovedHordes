using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImprovedHordes.Core.Threading
{
    public sealed class ThreadSubscription<T>
    {
        private readonly ConcurrentBag<ThreadSubscriber<T>> subscriptions = new ConcurrentBag<ThreadSubscriber<T>>();
        
        public ThreadSubscription()
        {
        }

        public ThreadSubscriber<T> Subscribe()
        {
            ThreadSubscriber<T> subscription = new ThreadSubscriber<T>();
            subscriptions.Add(subscription);

            return subscription;
        }

        public void Update(T recent)
        {
            foreach(var subscription in subscriptions)
            {
                subscription.Add(recent);
            }
        }
    }

    public class ThreadSubscriber<T>
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
