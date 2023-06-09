using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Threading
{
    public abstract class MainThreaded
    {
        private static readonly List<MainThreaded> instances = new List<MainThreaded>();

        public MainThreaded()
        {
            instances.Add(this);
        }

        public abstract void Update(float dt);

        public static void UpdateAll(float dt)
        {
            foreach(var instance in instances) 
            {
                instance.Update(dt);
            }
        }
    }
}
