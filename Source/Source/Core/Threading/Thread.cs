using ImprovedHordes.Source.Core.Threading;
using System.Threading;

namespace ImprovedHordes.Source.Utils
{
    public abstract class Thread
    {
        private readonly string name;
        private ThreadManager.ThreadInfo threadInfo;

        private LockedObject<bool,LockedObjectReader<bool>,LockedObjectWriter<bool>> running = new LockedObject<bool, LockedObjectReader<bool>, LockedObjectWriter<bool>>(false);

        public Thread(string name)
        {
            this.name = name;
        }

        public void ExecuteThread()
        {
            this.threadInfo = ThreadManager.StartThread(this.name, new ThreadManager.ThreadFunctionDelegate(StartThread), new ThreadManager.ThreadFunctionLoopDelegate(LoopThread), new ThreadManager.ThreadFunctionEndDelegate(EndThread), System.Threading.ThreadPriority.Normal, null, null, true);
        }

        public void ShutdownThread()
        {
            using(var runningWriter = this.running.Set(true))
            {
                runningWriter.Set(false);
            }
        }

        private void StartThread(ThreadManager.ThreadInfo threadInfo)
        {
            using(var runningWriter = this.running.Set(true))
            {
                runningWriter.Set(true);
            }

            this.OnStartup();
        }

        private int LoopThread(ThreadManager.ThreadInfo threadInfo)
        {
            while (true)
            {
                using (var runningReader = this.running.Get(false))
                {
                    if((runningReader.IsReading() && !runningReader.Get()) || !this.OnLoop())
                    {
                        break;
                    }
                }
            }

            using(var runningWriter = this.running.Set(true))
            {
                runningWriter.Set(false);
            }

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