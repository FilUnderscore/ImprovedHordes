namespace ImprovedHordes.Core.Threading.Request
{
    public abstract class AsyncMainThreadRequest : IMainThreadRequest
    {
        private bool complete;

        public abstract void TickExecute(float dt);

        public abstract bool IsDone();

        public void OnCleanup()
        {
            this.complete = true;
        }

        public bool IsComplete()
        {
            return this.complete;
        }
    }
}
