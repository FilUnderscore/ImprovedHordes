using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public class HordeCluster : IAIAgent
    {
        private readonly IHorde horde;
        private Vector3 location;
        private float density;
        private float densityPerEntity;

        private readonly List<HordeClusterEntity> entities = new List<HordeClusterEntity>();
        private bool spawned;

        private HordeClusterAIExecutor AIExecutor;

        public HordeCluster(IHorde horde, Vector3 location, float density)
        {
            this.horde = horde;
            this.location = location;
            this.density = density;

            this.spawned = false;

            this.AIExecutor = new HordeClusterAIExecutor(this);
        }

        public IHorde GetHorde()
        {
            return this.horde;
        }

        public Vector3 GetLocation()
        {
            return this.location;
        }

        public float GetDensity()
        {
            return this.density;
        }

        public void Spawn(PlayerHordeGroup group)
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                var request = new HordeSpawnRequest(horde, group, location, GetDensityToSpawn());
                instance.GetMainThreadRequestProcessor().RequestAndWait(request);

                List<HordeClusterEntity> entities = request.GetEntities().Select(entity => new HordeClusterEntity(entity)).ToList();
                this.AddEntities(entities);

                spawned = true;
            }
        }

        private void AddEntities(List<HordeClusterEntity> entities)
        {
            this.entities.AddRange(entities);
            this.AIExecutor.AddEntities(entities);

            this.RecalculateDensityPerEntity();
            this.AIExecutor.Notify(true);
        }

        private void RecalculateDensityPerEntity()
        {
            this.densityPerEntity = this.density / this.entities.Count;
        }

        private float GetDensityToSpawn()
        {
            if (this.entities.Count == 0 || this.densityPerEntity == 0.0f)
                return this.density;

            return this.density - (this.densityPerEntity * this.entities.Count);
        }

        public bool IsDensityMatchedWithEntityCount()
        {
            return this.spawned ? this.densityPerEntity * this.entities.Count >= this.density : true;
        }

        public void Despawn()
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                instance.GetMainThreadRequestProcessor().RequestAndWait(new HordeDespawnRequest(this.entities));
                this.entities.Clear();

                spawned = false;
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
                    this.density -= this.densityPerEntity;
                });
            }
        }

        public bool IsSpawned()
        {
            return this.spawned;
        }

        public bool IsDead()
        {
            return this.density <= 0.0f;
        }

        private bool merged;

        public bool Merge(HordeCluster cluster)
        {
            if (cluster.merged)
                return false;

            this.density += cluster.density;
            cluster.merged = true;

            if(cluster.spawned)
            {
                // Take control of cluster entities.
                this.AddEntities(cluster.entities);
                cluster.AIExecutor.Notify(false);
            }

            // Check when merging if both clusters have same objective.

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
            float speed = 1 * dt;
            Vector3 direction = (location - this.location).normalized;

            this.location += direction * speed;
        }

        public void Queue(bool interrupt = false, params AICommand[] commands)
        {
            this.AIExecutor.Queue(interrupt, commands);
        }
    }
}
