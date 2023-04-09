using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public sealed partial class WorldHordeClusterTracker
    {
        private abstract class HordeCluster
        {
            public readonly object Lock = new object();

            protected readonly WorldHordeSpawner spawner;
            protected readonly IHorde horde;
            protected PlayerHordeGroup nearbyPlayerGroup;
            protected float density;

            private bool stateSet = false;

            public HordeCluster(WorldHordeSpawner spawner, IHorde horde, float density)
            {
                this.spawner = spawner;
                this.horde = horde;
                this.density = density;
            }

            public IHorde GetHorde()
            {
                return this.horde;
            }

            public abstract Vector3 GetLocation();
            public abstract bool IsLoaded();

            public float GetEntityDensity()
            {
                return this.density;
            }

            public abstract IAIAgent[] GetAIAgents();

            public void SetNearbyPlayerGroup(PlayerHordeGroup playerGroup)
            {
                this.nearbyPlayerGroup = playerGroup;
            }

            public PlayerHordeGroup GetNearbyPlayerGroup()
            {
                return this.nearbyPlayerGroup;
            }

            public virtual void OnStateChange() { }

            public void SetNextStateSet(bool stateSet)
            {
                this.stateSet = stateSet;
            }

            public bool NextStateSet()
            {
                return this.stateSet;
            }

            public abstract HordeCluster Split(float density);
            public abstract void Recombine(HordeCluster cluster);
        }
    }
}