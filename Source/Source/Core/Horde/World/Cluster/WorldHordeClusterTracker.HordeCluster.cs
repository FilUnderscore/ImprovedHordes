using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public sealed partial class WorldHordeClusterTracker
    {
        private abstract class HordeCluster
        {
            protected readonly WorldHordeSpawner spawner;
            protected readonly IHorde horde;

            private bool stateSet = false;

            public HordeCluster(WorldHordeSpawner spawner, IHorde horde)
            {
                this.spawner = spawner;
                this.horde = horde;
            }

            public IHorde GetHorde()
            {
                return this.horde;
            }

            public abstract Vector3 GetLocation();
            public abstract bool IsLoaded();

            public abstract int GetEntityCount();
            public abstract IAIAgent[] GetAIAgents();

            public virtual void OnStateChange() { }

            public void SetNextStateSet(bool stateSet)
            {
                this.stateSet = stateSet;
            }

            public bool NextStateSet()
            {
                return this.stateSet;
            }

            public abstract HordeCluster Split(int size);
            public abstract void Recombine(HordeCluster cluster);
        }
    }
}