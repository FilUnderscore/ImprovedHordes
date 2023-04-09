using System.Threading;

namespace ImprovedHordes.Source.Utils
{
    public abstract class Thread
    {
        private readonly string name;
        private ThreadManager.ThreadInfo threadInfo;

        private bool running; // Atomic by nature.

        public Thread(string name)
        {
            this.name = name;
        }

        public void ExecuteThread()
        {
            this.threadInfo = ThreadManager.StartThread(this.name, new ThreadManager.ThreadFunctionDelegate(StartThread), new ThreadManager.ThreadFunctionLoopDelegate(LoopThread), new ThreadManager.ThreadFunctionEndDelegate(EndThread), System.Threading.ThreadPriority.Lowest, null, null, true);
        }

        public void ShutdownThread()
        {
            Monitor.Enter(this.running);
            this.running = false;
            Monitor.Exit(this.running);
        }

        private void StartThread(ThreadManager.ThreadInfo threadInfo)
        {
            running = true;

            this.OnStartup();
        }

        private int LoopThread(ThreadManager.ThreadInfo threadInfo)
        {
            bool entered;
            while ((entered = !Monitor.TryEnter(this.running)) || running)
            {
                if (!this.OnLoop())
                    break;

                if (entered)
                    Monitor.Exit(this.running);
            }

            this.running = false;
            Monitor.Exit(this.running);

            return -1;
        }

        private void EndThread(ThreadManager.ThreadInfo threadInfo, bool exitForException)
        {
            this.OnShutdown();
        }

        public virtual void OnStartup() { }
        public virtual void OnShutdown() { }

        public abstract bool OnLoop();
    }
}