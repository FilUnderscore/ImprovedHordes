using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.Abstractions.World.Random;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Core.Threading
{
    public abstract class Threaded
    {
        private static readonly Setting<int> THREAD_TICK_MS = new Setting<int>("thread_tick_ms", 100);
        private static readonly List<Threaded> instances = new List<Threaded>();

        private ThreadManager.ThreadInfo threadInfo;
        protected readonly IWorldRandom Random;

        protected readonly ILoggerFactory LoggerFactory;
        protected readonly Abstractions.Logging.ILogger Logger;

        private bool shutdown = false;

        public Threaded(ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory)
        {
            this.LoggerFactory = loggerFactory;
            this.Random = randomFactory.CreateRandom(this.GetType().FullName.GetHashCode());

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

        private float start;

        private int ThreadLoop(ThreadManager.ThreadInfo threadInfo)
        {
            int threadTickMs = THREAD_TICK_MS.Value;
            bool paused = false;

            while(!this.shutdown)
            {
                float end = Time.time;
                bool unpaused = !(!CanRun() || GameManager.Instance.IsPaused());

                if (!unpaused)
                {
                    paused = true;
                    return threadTickMs * 10;
                }
                else if(paused) // Since we were previously paused, don't calculate dt during that time.
                {
                    paused = false;
                    start = Time.time;
                }

                try
                {
                    float dt = end - start;
                    start = Time.time;

                    //this.Logger.Info("DeltaTime: " + dt);

                    UpdateAsync(dt);
                }
                catch(Exception e)
                {
                    this.Logger.Error($"An exception occurred during {nameof(UpdateAsync)}: {e.Message} \nStacktrace: \n{e.StackTrace}");
                }

                return threadTickMs;
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
