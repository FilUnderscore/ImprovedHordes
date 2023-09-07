using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.Abstractions.World.Random;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            bool isDebug = UnityEngine.Debug.isDebugBuild;

            if (!isDebug) // For standard builds of 7DTD.
            {
                this.threadInfo = ThreadManager.StartThread($"IHThreaded-{this.GetType().Name}", ThreadStart, ThreadLoop, ThreadEnd, System.Threading.ThreadPriority.Lowest, _useRealThread: true);
            }
            else // For debug builds of 7DTD. Conditional method groups are not supported in C# 7.3 so we can't stick this in one line.
            {
                this.stopwatch = Stopwatch.StartNew();
                this.threadInfo = ThreadManager.StartThread($"IHThreaded-Debug-{this.GetType().Name}", ThreadStart, ThreadLoopDebug, ThreadEnd, System.Threading.ThreadPriority.Lowest, _useRealThread: true);
            }
        }

        private void ThreadStart(ThreadManager.ThreadInfo threadInfo)
        {
            this.OnStart();
        }

        private Stopwatch stopwatch;
        private float start;

        private int UpdateThread(Action resetTime, Func<float> getDeltaTime)
        {
            int threadTickMs = THREAD_TICK_MS.Value;
            bool paused = false;

            while (!this.shutdown)
            {
                bool unpaused = !(!CanRun() || GameManager.Instance.IsPaused());

                if (!unpaused)
                {
                    paused = true;
                    return threadTickMs * 10;
                }
                else if (paused) // Since we were previously paused, don't calculate dt during that time.
                {
                    paused = false;
                    resetTime();
                }

                try
                {
                    float dt = getDeltaTime();
                    resetTime();

                    UpdateAsync(dt);
                }
                catch (Exception e)
                {
                    this.Logger.Error($"An exception occurred during {nameof(UpdateAsync)}: {e.Message} \nStacktrace: \n{e.StackTrace}");
                }

                return threadTickMs;
            }

            return -1;
        }

        private int ThreadLoop(ThreadManager.ThreadInfo _)
        {
            float end = Time.time;

            return UpdateThread(() =>
            {
                start = Time.time;
            }, () =>
            {
                return end - start;
            });
        }

        private int ThreadLoopDebug(ThreadManager.ThreadInfo threadInfo) // Consistent with ThreadLoop however may be slightly slower for debug builds due to using Stopwatch.
        {
            return UpdateThread(() =>
            {
                this.stopwatch.Restart();
            }, () =>
            {
                return this.stopwatch.ElapsedMilliseconds / 1000.0f;
            });
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
