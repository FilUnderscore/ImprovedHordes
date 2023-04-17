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

        private readonly List<HordeEntity> entities = new List<HordeEntity>();
        private bool spawned;

        private HordeAIExecutor AIExecutor;

        public WorldHorde(Vector3 location, IHorde horde, float density, params AICommand[] commands) : this(location, new HordeCluster(horde, density), commands) { }

        public WorldHorde(Vector3 location, HordeCluster cluster, params AICommand[] commands)
        {
            this.location = location;
            this.AddCluster(cluster);

            this.spawned = false;

            this.AIExecutor = new HordeAIExecutor(this);
            this.AIExecutor.Queue(false, commands);
        }

        public Vector3 GetLocation()
        {
            return this.location;
        }

        public void Spawn(PlayerHordeGroup group)
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                var request = new HordeSpawnRequest(this, this.clusters.Where(cluster => !cluster.IsSpawned()).ToList(), group);
                instance.GetMainThreadRequestProcessor().RequestAndWait(request);

                List<HordeEntity> entitiesToAdd = new List<HordeEntity>();
                foreach(var entry in request.GetEntities())
                {
                    var cluster = entry.Key;
                    
                    foreach(var entity in entry.Value)
                    {
                        entitiesToAdd.Add(new HordeEntity(cluster, entity));
                    }
                }

                this.AddEntities(entitiesToAdd);

                clusters.ForEach(cluster => cluster.SetSpawned(true));
                spawned = true;
            }
        }

        public bool HasClusterSpawnsWaiting()
        {
            return this.clusters.Any(cluster => !cluster.IsSpawned());
        }

        private void AddEntities(List<HordeEntity> entities)
        {
            this.entities.AddRange(entities);
            this.AIExecutor.AddEntities(entities);

            this.AIExecutor.Notify(true);
        }

        private void AddCluster(HordeCluster cluster)
        {
            clusters.Add(cluster);

            walkSpeed = clusters.Average(clusterEntry => clusterEntry.GetHorde().GetWalkSpeed());
            sensitivity = clusters.Max(clusterEntry => clusterEntry.GetHorde().GetSensitivity());
        }

        private void AddClusters(List<HordeCluster> clusters)
        {
            clusters.ForEach(cluster => AddCluster(cluster));
        }

        public void Despawn()
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                instance.GetMainThreadRequestProcessor().RequestAndWait(new HordeDespawnRequest(this.entities));
                this.entities.Clear();

                spawned = false;
                this.clusters.ForEach(cluster => cluster.SetSpawned(false));

                this.AIExecutor.Notify(spawned);
            }
        }

        public void UpdatePosition()
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                var request = new HordeUpdateRequest(this.entities);
                instance.GetMainThreadRequestProcessor().RequestAndWait(request);

                this.location = request.GetPosition();
                request.GetDead().ForEach(deadEntity =>
                {
                    this.entities.Remove(deadEntity);
                    deadEntity.GetCluster().NotifyDensityRemoved();

                    if (deadEntity.GetCluster().IsDead())
                        clusters.Remove(deadEntity.GetCluster());
                });
            }
        }

        public bool IsSpawned()
        {
            return this.spawned;
        }

        public bool IsDead()
        {
            return this.clusters.Count == 0;
        }

        private bool merged;

        public bool Merge(WorldHorde horde)
        {
            if (horde.merged || (horde.AIExecutor.CalculateObjectiveScore() < this.AIExecutor.CalculateObjectiveScore()))
                return false;

            this.AddClusters(horde.clusters);
            horde.merged = true;

            if(horde.spawned)
            {
                // Take control of horde entities.
                this.AddEntities(horde.entities);
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
