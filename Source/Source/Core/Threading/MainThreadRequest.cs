using System.Threading;

namespace ImprovedHordes.Source.Core.Threading
{
    public interface IMainThreadRequest
    {
        /// <summary>
        /// Execute per tick on main thread.
        /// </summary>
        void TickExecute();

        /// <summary>
        /// Is the request fulfilled? If so, notify waiting threads.
        /// </summary>
        /// <returns></returns>
        bool IsDone();

        void OnCleanup();
    }

    public abstract class BlockingMainThreadRequest : IMainThreadRequest
    {
        private readonly ManualResetEventSlim slim = new ManualResetEventSlim(false);

        /// <summary>
        /// Execute per tick on main thread.
        /// </summary>
        public abstract void TickExecute();

        /// <summary>
        /// Is the request fulfilled? If so, notify waiting threads.
        /// </summary>
        /// <returns></returns>
        public abstract bool IsDone();

        public virtual void OnCleanup() { }

        public void Notify()
        {
            this.slim.Set();
        }

        public void Wait()
        {
            this.slim.Wait();
        }

        public void Dispose()
        {
            this.slim.Dispose();
        }
    }
}
