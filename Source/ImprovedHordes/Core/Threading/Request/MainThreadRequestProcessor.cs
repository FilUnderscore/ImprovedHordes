using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ImprovedHordes.Core.Threading.Request
{
    public sealed class MainThreadRequestProcessor : MainThreaded
    {
        // Shared
        private readonly ConcurrentQueue<IMainThreadRequest> requests = new ConcurrentQueue<IMainThreadRequest>();

        // Private
        private readonly List<IMainThreadRequest> requestsBeingProcessed = new List<IMainThreadRequest>();
        private readonly List<IMainThreadRequest> requestsToRemove = new List<IMainThreadRequest>();

        protected override void Update(float dt)
        {
            while (!requests.IsEmpty && requests.TryDequeue(out IMainThreadRequest request))
            {
                requestsBeingProcessed.Add(request);
            }

            foreach (var request in requestsBeingProcessed)
            {
                request.TickExecute(dt);

                if(request.IsDone())
                {
                    request.OnCleanup();
                    requestsToRemove.Add(request);
                }
            }

            foreach (var request in requestsToRemove)
            {
                requestsBeingProcessed.Remove(request);

                if(request is BlockingMainThreadRequest blockingRequest)
                    blockingRequest.Notify();
            }
            requestsToRemove.Clear();
        }

        public void RequestAndWait(BlockingMainThreadRequest request)
        {
            requests.Enqueue(request);

            request.Wait();
            request.Dispose();
        }

        public void Request(IMainThreadRequest request)
        {
            requests.Enqueue(request);
        }

        public Dictionary<Type, int> GetRequestCounts()
        {
            Dictionary<Type, int> requestCounts = new Dictionary<Type, int>();
            List<IMainThreadRequest> requests = this.requestsBeingProcessed.ToList();

            foreach(var request in requests)
            {
                Type requestType = request.GetType();

                if(requestCounts.TryGetValue(requestType, out _))
                {
                    requestCounts[requestType] += 1;
                }
                else
                {
                    requestCounts.Add(requestType, 1);
                }
            }

            return requestCounts;
        }

        protected override void Shutdown()
        {
            requestsBeingProcessed.Clear();
            requestsToRemove.Clear();

            while (requests.TryDequeue(out _)) { }
        }
    }
}
