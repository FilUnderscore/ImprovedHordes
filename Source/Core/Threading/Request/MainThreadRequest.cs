namespace ImprovedHordes.Core.Threading.Request
{
    public interface IMainThreadRequest
    {
        /// <summary>
        /// Execute per tick on main thread.
        /// </summary>
        void TickExecute(float dt);

        /// <summary>
        /// Is the request fulfilled? If so, notify waiting threads.
        /// </summary>
        /// <returns></returns>
        bool IsDone();

        void OnCleanup();
    }
}