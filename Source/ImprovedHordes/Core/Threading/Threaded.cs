using ImprovedHordes.Core.Abstractions.Logging;
using System;
using System.Collections.Generic;

namespace ImprovedHordes.Core.Threading
{
    public abstract class Threaded
    {
        private static readonly List<Threaded> instances = new List<Threaded>();

        private ThreadManager.ThreadInfo threadInfo;
        protected readonly GameRandom Random;

        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;

        private bool shutdown = false;

        public Threaded(ILoggerFactory loggerFactory)
        {
            this.LoggerFactory = loggerFactory;

            int gameSeed = GameManager.Instance.World.Seed;
            this.Random = GameRandomManager.Instance.CreateGameRandom(gameSeed + this.GetType().FullName.GetHashCode());

            this.Logger = loggerFactory.Create(this.GetType());

            instances.Add(this);
        }

        protected void Start()
        {
            this.threadInfo = ThreadManager.StartThread($"IHThreaded-{this.GetType().Name}", ThreadStart, ThreadLoop, ThreadEnd, System.Threading.ThreadPriority.Lowest, _useRealThread: true);
        }

        private void ThreadStart(ThreadManager.ThreadInfo threadInfo)
        {
            this.OnStart();
        }
        
        private int ThreadLoop(ThreadManager.ThreadInfo threadInfo)
        {
            while(!this.shutdown)
            {
                if (!CanRun() || GameManager.Instance.IsPaused())
                    return 1000;

                try
                {
                    UpdateAsync(0.1f);
                }
                catch(Exception e)
                {
                    this.Logger.Error($"An exception occurred during {nameof(UpdateAsync)}: {e.Message} \nStacktrace: \n{e.StackTrace}");
                }

                return 100;
            }

            return -1;
        }

        private void ThreadEnd(ThreadManager.ThreadInfo threadInfo, bool exitForException)
        {
            this.OnShutdown();
        }

        protected virtual bool CanRun()
        {
            return true;
        }

        public void Shutdown()
        {
            this.shutdown = true;
        }

        protected abstract void UpdateAsync(float dt);
        protected virtual void OnStart() { }
        protected virtual void OnShutdown() { }

        internal static void StartAll()
        {
            foreach(var instance in instances)
            {
                instance.Start();
            }
        }

        internal static void ShutdownAll()
        {
            foreach(var instance in instances)
            {
                instance.Shutdown();
            }

            instances.Clear();
        }
    }
}
