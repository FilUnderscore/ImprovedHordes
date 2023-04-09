using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public sealed partial class WorldHordeClusterTracker
    {
        private sealed class UnloadedHordeCluster : HordeCluster, IAIAgent
        {
            private Vector3 location;
            private int size;

            public UnloadedHordeCluster(WorldHordeSpawner spawner, LoadedHordeCluster loadedHordeCluster) : this(spawner, loadedHordeCluster.GetHorde(), loadedHordeCluster.GetLocation(), loadedHordeCluster.GetEntityCount()) { }

            public UnloadedHordeCluster(WorldHordeSpawner spawner, IHorde horde, Vector3 location, int size) : base(spawner, horde)
            {
                this.location = location;
                this.size = size;
            }

            public override IAIAgent[] GetAIAgents()
            {
                return new IAIAgent[] { this };
            }

            public override int GetEntityCount()
            {
                return this.size;
            }

            public override Vector3 GetLocation()
            {
                return this.location;
            }

            public EntityAlive GetTarget()
            {
                return null;
            }

            public bool IsDead()
            {
                return this.size == 0;
            }

            public override bool IsLoaded()
            {
                return false;
            }

            public void MoveTo(Vector3 location, float dt)
            {
                this.location = Vector3.Lerp(this.location, location, dt);
            }

            public override void Recombine(HordeCluster cluster)
            {
                this.size += cluster.GetEntityCount();
            }

            public override HordeCluster Split(int size)
            {
                size = Mathf.Clamp(size, 0, this.size);
                this.size -= size;

                return new UnloadedHordeCluster(this.spawner, this.horde, this.location, size);
            }
        }
    }
}
