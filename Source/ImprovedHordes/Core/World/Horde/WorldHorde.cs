using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.AI;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Core.World.Horde.Cluster;
using ImprovedHordes.Core.World.Horde.Data;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.Core.World.Horde.Spawn.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde
{
    public sealed class WorldHorde : IAIAgent, ISaveable<WorldHordeData>
    {
        private Vector3 location;
        private HordeSpawnData spawnData;

        private readonly List<HordeCluster> clusters = new List<HordeCluster>();

        private readonly IRandomFactory<IWorldRandom> randomFactory;
        private readonly IWorldRandom worldRandom;

        private HordeCharacteristics characteristics = new HordeCharacteristics();

        private readonly IAICommandGenerator<AICommand> commandGenerator;
        private HordeAIExecutor AIExecutor;

        private int entityCount = 0; // Shared by main thread requests.
        private bool sleeping = false;

        public WorldHorde(Vector3 location, HordeSpawnData spawnData, IHorde horde, float density, IRandomFactory<IWorldRandom> randomFactory, IAICommandGenerator<AICommand> commandGenerator, IAICommandGenerator<EntityAICommand> entityCommandGenerator) : this(location, spawnData, new HordeCluster(horde, density * DetermineBiomeDensity(location), entityCommandGenerator), randomFactory, commandGenerator) { }

        public WorldHorde(Vector3 location, HordeSpawnData spawnData, HordeCluster cluster, IRandomFactory<IWorldRandom> randomFactory, IAICommandGenerator<AICommand> commandGenerator) : this(location, spawnData, randomFactory, commandGenerator)
        {
            this.AddCluster(cluster);
        }

        private WorldHorde(Vector3 location, HordeSpawnData spawnData, IRandomFactory<IWorldRandom> randomFactory, IAICommandGenerator<AICommand> commandGenerator)
        {
            this.location = location;
            this.spawnData = spawnData;

            this.randomFactory = randomFactory;
            this.worldRandom = randomFactory.CreateRandom(this.GetHashCode());

            this.commandGenerator = commandGenerator;
            this.AIExecutor = new HordeAIExecutor(this, this.worldRandom, commandGenerator);
        }

        public WorldHorde(WorldHordeData data, IRandomFactory<IWorldRandom> randomFactory) : this(data.GetLocation(), data.GetSpawnData(), randomFactory, data.GetCommandGenerator())
        {
            foreach(var cluster in data.GetClusters())
            {
                this.AddCluster(cluster);
            }
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

            float maxHordeDensity = DetermineBiomeDensity(this.location);
            if (otherHordeDensity + hordeDensity > maxHordeDensity)
                return false;

            return true;
        }

        public bool Split(ILoggerFactory loggerFactory, MainThreadRequestProcessor mainThreadRequestProcessor, out List<WorldHorde> newHordes)
        {
            if(!DoesHordeExceedBiomeDensity())
            {
                newHordes = null;
                return false;
            }

            newHordes = new List<WorldHorde>();

            float biomeDensity = DetermineBiomeDensity(this.location);
            float splitDensity = this.GetDensity() - biomeDensity;

            WorldHorde newHorde;
            for(int i = 0; i < this.clusters.Count; i++)
            {
                if (splitDensity <= 0.0f)
                    break;

                var cluster = this.clusters[i];

                if(cluster.GetDensity() > biomeDensity)
                {
                    // Split cluster.
                    float clusterSplitDensity = cluster.GetDensity() - biomeDensity;

                    do
                    {
                        newHorde = new WorldHorde(this.location, this.spawnData, this.randomFactory, this.commandGenerator);
                        HordeCluster currentCluster = new HordeCluster(cluster.GetHorde(), Mathf.Min(clusterSplitDensity, biomeDensity), cluster.GetEntityCommandGenerator());
                        newHorde.AddCluster(currentCluster);
                        newHordes.Add(newHorde);
                        clusterSplitDensity -= biomeDensity;
                    } while (clusterSplitDensity > 0.0f);
                }
                else
                {
                    newHorde = new WorldHorde(this.location, this.spawnData, this.randomFactory, this.commandGenerator);
                    newHorde.AddCluster(cluster);
                    newHordes.Add(newHorde);
                }

                this.clusters.RemoveAt(i--);
                splitDensity -= cluster.GetDensity();
            }

            // Respawn horde.
            if(this.IsSpawned())
                this.Despawn(loggerFactory, mainThreadRequestProcessor);

            return true;
        }

        private float GetDensity()
        {
            return this.clusters.Sum(cluster => cluster.GetDensity());
        }

        private bool DoesHordeExceedBiomeDensity()
        {
            return this.GetDensity() > DetermineBiomeDensity(this.location);
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

        private static float DetermineBiomeDensity(Vector3 location)
        {
            BiomeDefinition biome = GameManager.Instance.World.GetBiome((int)location.x, (int)location.z);

            if (biome == null)
                return 1.0f;

            float maxHordeDensity = WorldHordeTracker.MAX_HORDE_DENSITY.Value - 1.0f;
            return 1.0f + (maxHordeDensity - (maxHordeDensity / biome.Difficulty));
        }

        public bool AnyPlayersNearby(out float distance)
        {
            distance = 0.0f;
            return false;
        }

        public WorldHordeData GetData()
        {
            return new WorldHordeData(this.location, this.spawnData, this.clusters, this.characteristics, this.commandGenerator);
        }
    }
}
