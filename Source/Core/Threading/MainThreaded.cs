using System.Collections.Generic;

namespace ImprovedHordes.Core.Threading
{
    public abstract class MainThreaded
    {
        private static readonly List<MainThreaded> instances = new List<MainThreaded>();

        public MainThreaded()
        {
            instances.Add(this);
        }

        protected abstract void Update(float dt);
        protected abstract void Shutdown();

        internal static void UpdateAll(float dt)
        {
            foreach(var instance in instances) 
            {
                instance.Update(dt);
            }
        }

        internal static void ShutdownAll() 
        {
            foreach(var instance in instances)
            {
                instance.Shutdown();
            }
        }
    }
}
