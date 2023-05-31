﻿using System.Threading.Tasks;

namespace ImprovedHordes.Source.Core.Threading
{
    public abstract class MainThreadSynchronizedTask : MainThreadSynchronizedTask<object>
    {
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

    public abstract class MainThreadSynchronizedTask<TaskReturnType>
    {
        private Task<TaskReturnType> UpdateTask;

        protected virtual bool CanRun()
        {
            return true;
        }

        public void Update(float dt)
        {
            if (UpdateTask != null && UpdateTask.IsCompleted)
            {
                this.OnTaskFinish(UpdateTask.Result);
                UpdateTask = null;
            }

            if (!CanRun()) // Ensure we first cleanup after task finishes before and if starting the next one.
                return;

            if (UpdateTask == null)
            {
                this.BeforeTaskRestart();

                this.UpdateTask = Task.Run(() =>
                {
                    return UpdateAsync(dt);
                });
            }
        }

        /// <summary>
        /// Called before a task starts for the first time / restarts subsequently.
        /// </summary>
        protected abstract void BeforeTaskRestart();
        protected abstract void OnTaskFinish(TaskReturnType returnValue);

        protected abstract TaskReturnType UpdateAsync(float dt);
    }
}