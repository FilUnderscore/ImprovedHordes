using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
        private object clearLock = new object();

        public bool TryGet(out T next)
        {
            lock (clearLock)
            {
                if(history.IsEmpty)
                {
                    next = default(T);
                    return false;
                }    

                List<T> reverseList = history.ToList();
                reverseList.Reverse();

                next = reverseList[0];
                return true;
            }
        }

        public void Add(T item)
        {
            history.Enqueue(item);

            if(history.Count > 30)
            {
                lock(clearLock)
                {
                    while(history.Count > 30)
                    {
                        history.TryDequeue(out _);
                    }
                }
            }    
        }
    }
}
