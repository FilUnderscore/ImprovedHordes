using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;
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
        private HordeSpawnData spawnData;

        private readonly List<HordeCluster> clusters = new List<HordeCluster>();

        private HordeCharacteristics characteristics = new HordeCharacteristics();
        private HordeAIExecutor AIExecutor;

        public WorldHorde(Vector3 location, HordeSpawnData spawnData, IHorde horde, float density, IAICommandGenerator commandGenerator) : this(location, spawnData, new HordeCluster(horde, density), commandGenerator) { }

        public WorldHorde(Vector3 location, HordeSpawnData spawnData, HordeCluster cluster, IAICommandGenerator commandGenerator)
        {
            this.location = location;
            this.spawnData = spawnData;

            this.AddCluster(cluster);

            this.AIExecutor = new HordeAIExecutor(this, commandGenerator);
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

                yield return new HordeClusterSpawnRequest(this, this.spawnData, cluster, group, entity =>
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
            entity.GetCluster().AddEntity(entity);
            this.AIExecutor.AddEntity(entity, true);
        }

        private void AddCluster(HordeCluster cluster)
        {
            clusters.Add(cluster);

            this.characteristics.Merge(cluster.GetHorde().CreateCharacteristics());
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

            if (!this.CanClustersMerge(horde))
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

        private bool CanClustersMerge(WorldHorde other)
        {
            foreach(var otherCluster in other.clusters)
            {
                foreach (var cluster in this.clusters)
                {
                    if (!otherCluster.GetHorde().CanMergeWith(cluster.GetHorde()))
                        return false;
                }
            }

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
            float speed = this.GetCharacteristics().GetCharacteristic<WalkSpeedHordeCharacteristic>().GetWalkSpeed() * dt;
            Vector3 direction = (location - this.location).normalized;

            this.location += direction * speed;
        }

        public void Interrupt(params AICommand[] commands)
        {
            this.AIExecutor.Interrupt(commands);
        }

        public HordeCharacteristics GetCharacteristics()
        {
            return this.characteristics;
        }

        public List<HordeCluster> GetClusters()
        {
            return this.clusters;
        }
    }
}
