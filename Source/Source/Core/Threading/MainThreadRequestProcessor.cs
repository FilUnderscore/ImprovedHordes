using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Threading
{
    public sealed class MainThreadRequestProcessor
    {
        // Shared
        private readonly ConcurrentQueue<IMainThreadRequest> requests = new ConcurrentQueue<IMainThreadRequest>();

        // Private
        private readonly List<IMainThreadRequest> requestsBeingProcessed = new List<IMainThreadRequest>();
        private readonly List<IMainThreadRequest> requestsToRemove = new List<IMainThreadRequest>();

        public void Update()
        {
            while (requests.TryDequeue(out IMainThreadRequest request))
            {
                requestsBeingProcessed.Add(request);
            }

            foreach (var request in requestsBeingProcessed)
            {
                request.TickExecute();

                if(request.IsDone())
                {
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

        public int GetRequestCount()
        {
            return this.requestsBeingProcessed.Count;
        }
    }
}
