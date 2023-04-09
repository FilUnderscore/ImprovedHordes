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

            public UnloadedHordeCluster(WorldHordeSpawner spawner, LoadedHordeCluster loadedHordeCluster) : this(spawner, loadedHordeCluster.GetHorde(), loadedHordeCluster.GetLocation(), loadedHordeCluster.GetEntityDensity()) { }

            public UnloadedHordeCluster(WorldHordeSpawner spawner, IHorde horde, Vector3 location, float density) : base(spawner, horde, density)
            {
                this.location = location;
            }

            public override IAIAgent[] GetAIAgents()
            {
                return new IAIAgent[] { this };
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
                return this.density <= 0.0f;
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
                this.density += cluster.GetEntityDensity();
            }

            public override HordeCluster Split(float density)
            {
                float newDensity = Mathf.Clamp(this.density - density, 0, this.density);
                this.density = newDensity;

                return new UnloadedHordeCluster(this.spawner, this.horde, this.location, density);
            }
        }
    }
}
