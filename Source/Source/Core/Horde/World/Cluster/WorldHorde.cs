using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde.AI;
using System;
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

        public IEnumerable<HordeClusterSpawnRequest> RequestSpawns(PlayerHordeGroup group, MainThreadRequestProcessor mainThreadRequestProcessor, Action<Entity> onSpawn)
        {
            foreach(var cluster in this.clusters)
            {
                if (cluster.IsSpawned())
                    continue;

                yield return new HordeClusterSpawnRequest(this, this.spawnData, cluster, group, entity =>
                {
                    this.AddEntity(new HordeClusterEntity(cluster, entity, this.characteristics), mainThreadRequestProcessor);

                    if (onSpawn != null)
                        onSpawn(entity);
                });

                cluster.SetSpawned(true);
            }
        }

        public bool HasClusterSpawnsWaiting()
        {
            return this.clusters.Any(cluster => !cluster.IsSpawned());
        }

        private void AddEntity(HordeClusterEntity entity, MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            entity.GetCluster().AddEntity(entity);
            this.AIExecutor.AddEntity(entity, mainThreadRequestProcessor);
        }

        private void AddCluster(HordeCluster cluster)
        {
            clusters.Add(cluster);

            this.characteristics.Merge(cluster.GetHorde().CreateCharacteristics());
        }

        public void Despawn(MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            mainThreadRequestProcessor.Request(new HordeDespawnRequest(this));

            this.clusters.ForEach(cluster => cluster.SetSpawned(false));
            this.AIExecutor.NotifyEntities(false, null);
        }

        private HordeUpdateRequest previousRequest;

        public void UpdatePosition(MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            if(previousRequest != null)
            {
                if (!previousRequest.IsComplete())
                    return;

                this.location = previousRequest.GetPosition();
                previousRequest.GetDead().ForEach(deadEntity =>
                {
                    deadEntity.GetCluster().RemoveEntity(deadEntity);
                    deadEntity.GetCluster().NotifyDensityRemoved();

                    if (deadEntity.GetCluster().IsDead())
                        clusters.Remove(deadEntity.GetCluster());
                });
            }

            previousRequest = new HordeUpdateRequest(this);
            mainThreadRequestProcessor.Request(previousRequest);
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
                horde.AIExecutor.NotifyEntities(false, null);
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
