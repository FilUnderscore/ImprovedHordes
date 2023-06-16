using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.AI;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Core.World.Horde.Cluster;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.Core.World.Horde.Spawn.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde
{
    public sealed class WorldHorde : IAIAgent
    {
        private Vector3 location;
        private HordeSpawnData spawnData;

        private readonly List<HordeCluster> clusters = new List<HordeCluster>();
        private readonly IWorldRandom worldRandom;

        private HordeCharacteristics characteristics = new HordeCharacteristics();
        private HordeAIExecutor AIExecutor;

        private int entityCount = 0; // Shared by main thread requests.
        private bool sleeping = false;

        public WorldHorde(Vector3 location, HordeSpawnData spawnData, IHorde horde, float density, IRandomFactory<IWorldRandom> randomFactory, IAICommandGenerator<AICommand> commandGenerator, IAICommandGenerator<EntityAICommand> entityCommandGenerator) : this(location, spawnData, new HordeCluster(horde, density, entityCommandGenerator), randomFactory, commandGenerator) { }

        public WorldHorde(Vector3 location, HordeSpawnData spawnData, HordeCluster cluster, IRandomFactory<IWorldRandom> randomFactory, IAICommandGenerator<AICommand> commandGenerator)
        {
            this.location = location;
            this.spawnData = spawnData;

            this.AddCluster(cluster);

            this.worldRandom = randomFactory.CreateRandom(this.GetHashCode());
            this.AIExecutor = new HordeAIExecutor(this, this.worldRandom, commandGenerator);
        }

        public Vector3 GetLocation()
        {
            return this.location;
        }

        public void RequestSpawns(WorldHordeSpawner spawner, PlayerHordeGroup group, MainThreadRequestProcessor mainThreadRequestProcessor, IWorldRandom worldRandom, Action<IEntity> onSpawn)
        {
            foreach(var cluster in this.clusters)
            {
                RequestSpawn(cluster, spawner, group, mainThreadRequestProcessor, worldRandom, onSpawn);
            }
        }

        public void RequestSpawn(HordeCluster cluster, WorldHordeSpawner spawner, PlayerHordeGroup group, MainThreadRequestProcessor mainThreadRequestProcessor, IWorldRandom worldRandom, Action<IEntity> onSpawn)
        {
            cluster.RequestSpawn(this, this.spawnData, spawner, group, mainThreadRequestProcessor, worldRandom, this.AIExecutor, onSpawn);
        }

        private void AddCluster(HordeCluster cluster)
        {
            clusters.Add(cluster);

            this.characteristics.Merge(cluster.GetHorde().CreateCharacteristics());
        }

        public void Despawn(ILoggerFactory loggerFactory, MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            mainThreadRequestProcessor.Request(new HordeDespawnRequest(loggerFactory, this, () =>
            {
                this.clusters.ForEach(cluster => cluster.SetSpawnState(HordeCluster.SpawnState.DESPAWNED));
                this.AIExecutor.NotifyEntities(false, null);
            }));
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
                    deadEntity.GetCluster().RemoveEntity(this, deadEntity);
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
            return this.clusters.Any(cluster => cluster.GetSpawnState() != HordeCluster.SpawnState.DESPAWNED);
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

            float otherHordeDensity = other.clusters.Sum(cluster => cluster.GetDensity());
            float hordeDensity = this.clusters.Sum(cluster => cluster.GetDensity());

            if (otherHordeDensity + hordeDensity > WorldHordeTracker.MAX_HORDE_DENSITY.Value)
                return false;

            return true;
        }

        public void Update(float dt)
        {
            this.AIExecutor.Update(dt);
        }

        public IEntity GetTarget()
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

        public void Sleep()
        {
            this.sleeping = true;
        }

        public void WakeUp()
        {
            this.sleeping = false;
        }

        public bool IsSleeping()
        {
            return this.sleeping;
        }

        public int GetSpawnedHordeEntityCount()
        {
            return this.entityCount;
        }

        public void SetSpawnedHordeEntityCount(int entityCount)
        {
            this.entityCount = entityCount;
        }

        public void Cleanup(IRandomFactory<IWorldRandom> randomFactory)
        {
            randomFactory.FreeRandom(this.worldRandom);
        }
    }
}
