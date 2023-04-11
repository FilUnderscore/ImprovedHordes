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
            private readonly Dictionary<int, Entity> entities;
            private readonly List<int> entitiesToRemove = new List<int>();

            private float densityPerEntity;

            private Vector3? cachedLocation;
            private double lastUpdatedCachedLocation;

            public LoadedHordeCluster(WorldHordeSpawner spawner, UnloadedHordeCluster unloadedHordeCluster) : this(spawner, unloadedHordeCluster.GetHorde(), unloadedHordeCluster.GetLocation(), unloadedHordeCluster.GetEntityDensity())
            {
                this.nearbyPlayerGroup = unloadedHordeCluster.GetNearbyPlayerGroup();
            }

            private LoadedHordeCluster(WorldHordeSpawner spawner, IHorde horde, Vector3 location, float density) : base(spawner, horde, density)
            {
                this.entities = new Dictionary<int, Entity>();
                this.GenerateEntities(location, density);
            }

            private LoadedHordeCluster(WorldHordeSpawner spawner, IHorde horde, float density, Dictionary<int, Entity> entities) : base(spawner, horde, density)
            {
                this.entities = entities;
            }

            private void GenerateEntities(Vector3 location, float density)
            {
                HordeEntitySpawnRequest request = new HordeEntitySpawnRequest(this.GetHorde(), this.nearbyPlayerGroup, location, density);
                this.spawner.Request(request);

                foreach (var entity in request.GetEntities())
                {
                    this.entities.Add(entity.entityId, new Entity(entity));
                }

                Log.Out("Generated: " + this.entities.Count);
                this.densityPerEntity = this.density / this.entities.Count;
            }

            public void Notify(List<int> entities)
            {
                foreach (int entityId in entities)
                {
                    if(this.entities.ContainsKey(entityId))
                    {
                        entitiesToRemove.Add(entityId);
                    }
                }

                if (entitiesToRemove.Count == 0)
                    return;

                Log.Out("Notifying entity killed in cluster");

                foreach (var entity in entitiesToRemove)
                {
                    this.entities.Remove(entity);
                    
                    this.density -= this.densityPerEntity;
                    Log.Out("Killed entity");
                }

                entitiesToRemove.Clear();

                this.density = Mathf.Max(this.density, 0.0f);
                Log.Out("New density: " + this.density + " new entity density: " + this.densityPerEntity);
            }

            public override void OnStateChange()
            {
                this.spawner.Request(new HordeDespawnRequest(this.entities.Values.Select(entity => entity.GetEntityInstance()).ToList()));
            }

            public override HordeCluster Split(float density)
            {
                int size = Mathf.Clamp(Mathf.FloorToInt(this.entities.Count * density), 0, this.entities.Count);

                var entities = this.entities.Take(size).ToList();
                entities.ForEach(entry => this.entities.Remove(entry.Key));

                this.density -= density;

                return new LoadedHordeCluster(this.spawner, this.horde, density, entities.ToDictionary(key => key.Key, value => value.Value));
            }

            public override void Recombine(HordeCluster horde)
            {
                if (horde is LoadedHordeCluster lhc)
                {
                    foreach(var entry in lhc.entities)
                        this.entities.Add(entry.Key, entry.Value);
                }
                else
                {
                    this.GenerateEntities(horde.GetLocation(), horde.GetEntityDensity());
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

                foreach (Entity entity in this.entities.Values)
                {
                    if(entity.IsDead())
                    {
                        entitiesToRemove.Add(entity.GetEntityInstance().entityId);
                        continue;
                    }

                    avgPos += entity.GetLocation();
                }

                this.cachedLocation = avgPos / this.entities.Count;
                return this.cachedLocation.Value;
            }

            public override IAIAgent[] GetAIAgents()
            {
                return this.entities.Values.ToArray<IAIAgent>();
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

                public bool CanInterrupt()
                {
                    return this.GetTarget() == null || !(this.GetTarget() is EntityPlayer);
                }
            }
        }
    }
}