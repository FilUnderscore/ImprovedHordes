﻿using System.Threading.Tasks;

namespace ImprovedHordes.Core.Threading
{
    public abstract class MainThreadSynchronizedTask : MainThreadSynchronizedTask<object>
    {
        public MainThreadSynchronizedTask() : base() { }

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
        private Task UpdateTask;
        private bool shutdown = false;

        public MainThreadSynchronizedTask()
        {
            int gameSeed = GameManager.Instance.World.Seed;
            this.Random = GameRandomManager.Instance.CreateGameRandom(gameSeed + this.GetType().FullName.GetHashCode());
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