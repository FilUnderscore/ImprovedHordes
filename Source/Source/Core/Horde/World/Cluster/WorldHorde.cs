using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class WorldHorde : IAIAgent
    {
        private Vector3 location;
        private readonly List<HordeCluster> clusters = new List<HordeCluster>();

        private float walkSpeed = 0.0f;
        private float sensitivity = 0.0f;

        private HordeAIExecutor AIExecutor;

        public WorldHorde(Vector3 location, IHorde horde, float density, params AICommand[] commands) : this(location, new HordeCluster(horde, density), commands) { }

        public WorldHorde(Vector3 location, HordeCluster cluster, params AICommand[] commands)
        {
            this.location = location;
            this.AddCluster(cluster);

            this.AIExecutor = new HordeAIExecutor(this);
            this.AIExecutor.Queue(false, commands);
        }

        public Vector3 GetLocation()
        {
            return this.location;
        }

        public IEnumerable<HordeClusterSpawnRequest> RequestSpawns(PlayerHordeGroup group)
        {
            foreach(var cluster in this.clusters)
            {
                if (cluster.IsSpawned())
                    continue;

                yield return new HordeClusterSpawnRequest(this, cluster, group, entity =>
                {
                    this.AddEntity(new HordeClusterEntity(cluster, entity));
                });

                cluster.SetSpawned(true);
            }
        }

        public bool HasClusterSpawnsWaiting()
        {
            return this.clusters.Any(cluster => !cluster.IsSpawned());
        }

        private void AddEntity(HordeClusterEntity entity)
        {
            Log.Out("Add");

            entity.GetCluster().AddEntity(entity);
            this.AIExecutor.AddEntity(entity, true);
        }

        private void AddCluster(HordeCluster cluster)
        {
            clusters.Add(cluster);

            walkSpeed = clusters.Average(clusterEntry => clusterEntry.GetHorde().GetWalkSpeed());
            sensitivity = clusters.Max(clusterEntry => clusterEntry.GetHorde().GetSensitivity());
        }

        public void Despawn()
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                instance.GetMainThreadRequestProcessor().RequestAndWait(new HordeDespawnRequest(this));

                this.clusters.ForEach(cluster => cluster.SetSpawned(false));
                this.AIExecutor.Notify(false);
            }
        }

        public void UpdatePosition()
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                var request = new HordeUpdateRequest(this);
                instance.GetMainThreadRequestProcessor().RequestAndWait(request);

                this.location = request.GetPosition();
                request.GetDead().ForEach(deadEntity =>
                {
                    deadEntity.GetCluster().RemoveEntity(deadEntity);
                    deadEntity.GetCluster().NotifyDensityRemoved();

                    if (deadEntity.GetCluster().IsDead())
                        clusters.Remove(deadEntity.GetCluster());
                });
            }
        }

        public bool IsDead()
        {
            return this.clusters.Count == 0;
        }

        public bool IsSpawned()
        {
            return this.clusters.Any(cluster => cluster.IsSpawned());
        }

        private void AddClusters(List<HordeCluster> clusters)
        {
            clusters.ForEach(cluster =>
            {
                this.AddCluster(cluster);
            });
        }

        private bool merged;

        public bool Merge(WorldHorde horde)
        {
            if (horde.merged || (horde.AIExecutor.CalculateObjectiveScore() < this.AIExecutor.CalculateObjectiveScore()))
                return false;

            this.AddClusters(horde.clusters);
            horde.merged = true;

            if(horde.IsSpawned())
            {
                horde.AIExecutor.Notify(false);
            }

            // Check when merging if both hordes have same objective.

            return true;
        }

        public void Update(float dt)
        {
            this.AIExecutor.Update(dt);
        }

        public bool CanInterrupt()
        {
            return true;
        }

        public EntityAlive GetTarget()
        {
            return null;
        }

        public void MoveTo(Vector3 location, float dt)
        {
            float speed = this.walkSpeed * dt;
            Vector3 direction = (location - this.location).normalized;

            this.location += direction * speed;
        }

        public void Queue(bool interrupt = false, params AICommand[] commands)
        {
            this.AIExecutor.Queue(interrupt, commands);
        }

        public float GetSensitivity()
        {
            return this.sensitivity;
        }

        public List<HordeCluster> GetClusters()
        {
            return this.clusters;
        }
    }
}
