using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public sealed partial class WorldHordeClusterTracker
    {
        private sealed class LoadedHordeCluster : HordeCluster
        {
            private const double CACHED_LOCATION_SECONDS = 1.0;

            private readonly List<Entity> entities;

            private Vector3? cachedLocation;
            private double lastUpdatedCachedLocation;

            public LoadedHordeCluster(WorldHordeSpawner spawner, UnloadedHordeCluster unloadedHordeCluster) : this(spawner, unloadedHordeCluster.GetHorde(), unloadedHordeCluster.GetLocation(), unloadedHordeCluster.GetEntityCount())
            {
            }

            public LoadedHordeCluster(WorldHordeSpawner spawner, IHorde horde, Vector3 location, int size) : base(spawner, horde)
            {
                this.entities = new List<Entity>();
                this.GenerateEntities(location, size);
            }

            private LoadedHordeCluster(WorldHordeSpawner spawner, IHorde horde, List<Entity> entities) : base(spawner, horde)
            {
                this.entities = entities;
            }

            private void GenerateEntities(Vector3 location, int size)
            {
                HordeEntitySpawnRequest request = new HordeEntitySpawnRequest(this.GetHorde(), location, size);
                this.spawner.Request(request);

                foreach (var entity in request.GetEntities())
                {
                    this.entities.Add(new Entity(entity));
                }
            }

            public override void OnStateChange()
            {
                this.spawner.Request(new HordeDespawnRequest(this.entities.Select(entity => entity.GetEntityInstance()).ToList()));
                this.entities.Clear();
            }

            public override HordeCluster Split(int size)
            {
                size = Mathf.Clamp(size, 0, this.entities.Count);

                List<Entity> entities = this.entities.Take(size).ToList();
                this.entities.RemoveRange(0, size);

                return new LoadedHordeCluster(this.spawner, this.horde, this.entities);
            }

            public override void Recombine(HordeCluster horde)
            {
                if (horde is LoadedHordeCluster lhc)
                {
                    this.entities.AddRange(lhc.entities);
                }
                else
                {
                    this.GenerateEntities(horde.GetLocation(), horde.GetEntityCount());
                }
            }

            public override bool IsLoaded()
            {
                return true;
            }

            public override Vector3 GetLocation()
            {
                if (cachedLocation == null)
                    return CalculateAverageLocation();

                double currentTime = Time.timeAsDouble;

                if (currentTime - lastUpdatedCachedLocation < CACHED_LOCATION_SECONDS)
                    return cachedLocation.Value;
                else
                    return CalculateAverageLocation();
            }

            private Vector3 CalculateAverageLocation()
            {
                lastUpdatedCachedLocation = Time.timeAsDouble;
                Vector3 avgPos = Vector3.zero;

                foreach (Entity entity in this.entities)
                {
                    avgPos += entity.GetLocation();
                }

                this.cachedLocation = avgPos / this.entities.Count;
                return this.cachedLocation.Value;
            }

            public override int GetEntityCount()
            {
                return this.entities.Count;
            }

            public override IAIAgent[] GetAIAgents()
            {
                return this.entities.ToArray<IAIAgent>();
            }

            private class Entity : IAIAgent
            {
                private EntityAlive entity;

                public Entity(EntityAlive entity)
                {
                    this.entity = entity;
                }

                public EntityAlive GetEntityInstance()
                {
                    return this.entity;
                }

                public bool IsDead()
                {
                    return this.entity.IsDead();
                }

                public Vector3 GetLocation()
                {
                    return this.entity.position;
                }

                public EntityAlive GetTarget()
                {
                    return this.entity.GetAttackTarget();
                }

                public void MoveTo(Vector3 location, float dt)
                {
                    this.entity.SetInvestigatePosition(location, 6000, false);
                    AstarManager.Instance.AddLocationLine(this.entity.position, location, 64);
                }
            }
        }
    }
}