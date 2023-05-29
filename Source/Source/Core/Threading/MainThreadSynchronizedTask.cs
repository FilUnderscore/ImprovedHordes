using System.Threading.Tasks;

namespace ImprovedHordes.Source.Core.Threading
{
    public abstract class MainThreadSynchronizedTask : MainThreadSynchronizedTask<object>
    {
        public override void OnTaskFinish(object returnValue)
        {
            OnTaskFinish();
        }

        public abstract void OnTaskFinish();

        public override async Task<object> UpdateAsync(float dt)
        {
            await UpdateAsync();
            return null;
        }

        public abstract Task UpdateAsync();
    }

    public abstract class MainThreadSynchronizedTask<TaskReturnType>
    {
        private Task<TaskReturnType> UpdateTask;

        public virtual bool CanRun()
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
                this.UpdateTask = Task.Run(async () =>
                {
                    return await UpdateAsync(dt);
                });
            }
        }

        /// <summary>
        /// Called before a task starts for the first time / restarts subsequently.
        /// </summary>
        public abstract void BeforeTaskRestart();
        public abstract void OnTaskFinish(TaskReturnType returnValue);

        public abstract Task<TaskReturnType> UpdateAsync(float dt);
    }
}
