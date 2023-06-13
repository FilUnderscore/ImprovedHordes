using ImprovedHordes.Core.Abstractions.Logging;
using System;
using System.Threading.Tasks;

namespace ImprovedHordes.Core.Threading
{
    public abstract class MainThreadSynchronizedTask : MainThreadSynchronizedTask<object>
    {
        public MainThreadSynchronizedTask(ILoggerFactory loggerFactory) : base(loggerFactory) { }

        protected override void OnTaskFinish(object returnValue)
        {
            OnTaskFinish();
        }

        protected abstract void OnTaskFinish();

        protected override object UpdateAsync(float dt)
        {
            UpdateAsyncVoid(dt);
            return null;
        }

        protected abstract void UpdateAsyncVoid(float dt);
    }

    public abstract class MainThreadSynchronizedTask<TaskReturnType> : MainThreaded
    {
        protected readonly GameRandom Random;

        protected readonly ILoggerFactory LoggerFactory;
        protected readonly ILogger Logger;

        private Task UpdateTask;
        private bool shutdown = false;

        public MainThreadSynchronizedTask(ILoggerFactory loggerFactory)
        {
            this.LoggerFactory = loggerFactory;

            int gameSeed = GameManager.Instance.World.Seed;
            this.Random = GameRandomManager.Instance.CreateGameRandom(gameSeed + this.GetType().FullName.GetHashCode());

            this.Logger = loggerFactory.Create(this.GetType());
        }
        
        protected virtual bool CanRun()
        {
            return true;
        }

        protected override void Update(float dt)
        {
            if (UpdateTask != null && UpdateTask.IsCompleted)
            {
                UpdateTask = null;
            }

            if (!CanRun() || GameManager.Instance.IsPaused() || this.shutdown) // Ensure we first cleanup after task finishes before and if starting the next one.
                return;

            if (UpdateTask == null)
            {
                this.UpdateTask = Task.Run(() =>
                {
                    this.BeforeTaskRestart();

                    TaskReturnType returnType = UpdateAsync(dt);

                    this.OnTaskFinish(returnType);
                });

                this.UpdateTask.ContinueWith(t => 
                {
                    AggregateException e = t.Exception.Flatten();

                    int exIndex = 0;
                    foreach(var ex in e.InnerExceptions)
                    {
                        this.Logger.Error($"#{++exIndex} - An exception occurred during {nameof(UpdateTask)}: {ex.Message} \nStacktrace: \n{ex.StackTrace}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        protected override void Shutdown()
        {
            this.shutdown = true;
        }

        /// <summary>
        /// Called before a task starts for the first time / restarts subsequently.
        /// </summary>
        protected abstract void BeforeTaskRestart();
        protected abstract void OnTaskFinish(TaskReturnType returnValue);

        protected abstract TaskReturnType UpdateAsync(float dt);
    }
}
