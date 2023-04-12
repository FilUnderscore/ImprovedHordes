using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Threading
{
    public sealed class MainThreadRequestProcessor
    {
        // Shared
        private readonly ConcurrentQueue<MainThreadRequest> requests = new ConcurrentQueue<MainThreadRequest>();

        // Private
        private readonly List<MainThreadRequest> requestsBeingProcessed = new List<MainThreadRequest>();
        private readonly List<MainThreadRequest> requestsToRemove = new List<MainThreadRequest>();

        public void Update()
        {
            while (requests.TryDequeue(out MainThreadRequest request))
            {
                requestsBeingProcessed.Add(request);
            }

            foreach (var request in requestsBeingProcessed)
            {
                if (!request.IsDone())
                {
                    request.TickExecute();
                }
                else
                {
                    requestsToRemove.Add(request);
                }
            }

            foreach (var request in requestsToRemove)
            {
                requestsBeingProcessed.Remove(request);
                request.Notify();
            }
            requestsToRemove.Clear();
        }

        public void RequestAndWait(MainThreadRequest request)
        {
            requests.Enqueue(request);

            request.Wait();
            request.Dispose();
        }
    }
}
