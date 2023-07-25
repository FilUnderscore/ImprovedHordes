using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Core.World.Horde.Spawn.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Cluster
{
    public sealed class HordeClusterEntity : IEntity
    {
        private readonly HordeCluster cluster;
        private IEntity entity;

        private readonly int entityClassId;
        private int entityId; // Entity ID can always change as another entity can take the ID when despawned.
        private bool spawned, awaitingSpawnStateChange, hordeDespawned, sleeping;
        private Vector3 location;

        private readonly HordeCharacteristics characteristics;

        public HordeClusterEntity(HordeCluster cluster, int entityClassId, Vector3 location, HordeCharacteristics characteristics)
        {
            this.cluster = cluster;
            this.entity = null;

            this.spawned = false;

            this.entityClassId = entityClassId;
            this.entityId = EntityFactory.nextEntityID++;
            this.location = location;

            this.characteristics = characteristics;
        }

        public HordeClusterEntity(HordeCluster cluster, IEntity entity, HordeCharacteristics characteristics)
        {
            this.cluster = cluster;
            this.entity = entity;

            this.spawned = true;

            this.entityClassId = entity.GetEntityClassId();
            this.entityId = entity.GetEntityId();
            this.location = entity.GetLocation();

            this.characteristics = characteristics;
        }

        public HordeCluster GetCluster()
        {
            return this.cluster;
        }

        public IEntity GetEntity()
        {
            return this.entity;
        }

        public Vector3 GetLocation()
        {
            return this.spawned ? this.entity.GetLocation() : this.location;
        }

        public IEntity GetTarget()
        {
            return this.spawned ? this.entity.GetTarget() : null;
        }

        public bool IsDead()
        {
            return this.spawned ? this.entity.IsDead() : false;
        }

        public void MoveTo(Vector3 location, bool aggro, float dt)
        {
            if(this.spawned)
            {
                this.MoveToSpawned(location, aggro);
            }
            else
            {
                this.MoveToDespawned(location, dt);
            }
        }

        public void Stop()
        {
            if (!this.spawned)
                return;

            this.entity.Stop();
        }

        public bool IsMoving()
        {
            if (!this.spawned)
                return false;

            return this.entity.IsMoving();
        }

        private void MoveToSpawned(Vector3 location, bool aggro)
        {
            Vector3 directionWithinLoadDistance = (location - this.GetLocation()).normalized;
            Vector3 locationWithinLoadDistance = (directionWithinLoadDistance * WorldHordeTracker.MAX_SPAWN_VIEW_DISTANCE) + this.GetLocation();

            this.entity.MoveTo(locationWithinLoadDistance, aggro, 0);
        }

        private void MoveToDespawned(Vector3 location, float dt)
        {
            float speed = this.characteristics.GetCharacteristic<WalkSpeedHordeCharacteristic>().GetWalkSpeed() * dt;
            Vector3 direction = (location - this.location).normalized;

            this.location += direction * speed;
        }

        public bool IsSpawned()
        {
            return this.spawned;
        }

        public bool IsAwaitingSpawnStateChange()
        {
            return this.awaitingSpawnStateChange;
        }

        public void RequestDespawn(MainThreadRequestProcessor mainThreadRequestProcessor, Action<IEntity> onDespawn)
        {
            this.awaitingSpawnStateChange = true;
            mainThreadRequestProcessor.Request(new HordeEntityDespawnRequest(this, onDespawn));
        }

        public void RequestSpawn(ILoggerFactory loggerFactory, IEntitySpawner spawner, MainThreadRequestProcessor mainThreadRequestProcessor, WorldHorde horde, PlayerHordeGroup playerGroup, Action<IEntity> onSpawn)
        {
            this.awaitingSpawnStateChange = true;
            mainThreadRequestProcessor.Request(new HordeEntitySpawnRequest(loggerFactory, spawner, horde, playerGroup, this, onSpawn));
        }

        public void Despawn()
        {
            this.location = this.entity.GetLocation();
            GameManager.Instance.World.RemoveEntity(this.entity.GetEntityId(), EnumRemoveEntityReason.Killed);

            this.entity = null;
            this.spawned = false;
            this.awaitingSpawnStateChange = false;
        }

        public bool Respawn(Abstractions.Logging.ILogger logger, IEntitySpawner spawner, Vector3 location)
        {
            if(!spawner.TrySpawnAt(this.entityClassId, this.entityId, location, out this.entity))
            {
                logger.Warn($"Failed to respawn HordeClusterEntity near {this.location}.");
                this.awaitingSpawnStateChange = false;

                return false;
            }

            this.entityId = this.entity.GetEntityId();
            this.location = this.entity.GetLocation();
            this.spawned = true;

            if (this.hordeDespawned)
            {
                this.Despawn();
                return false;
            }
            else
                this.awaitingSpawnStateChange = false;

            return true;
        }

        public void NotifyHordeDespawned()
        {
            this.hordeDespawned = true;
        }

        public int GetEntityId()
        {
            return this.entityId;
        }

        public int GetEntityClassId()
        {
            return this.entityClassId;
        }

        public void PlaySound(string soundName)
        {
            this.entity.PlaySound(soundName);
        }

        public string GetAlertSound()
        {
            return this.entity.GetAlertSound();
        }

        public bool IsStunned()
        {
            return this.entity.IsStunned();
        }

        public bool IsPlayer()
        {
            return false;
        }

        public void Sleep()
        {
            if (this.entity != null)
                this.entity.Sleep();
            else
                this.sleeping = true;
        }

        public void WakeUp()
        {
            if (this.entity != null)
                this.entity.WakeUp();
            else
                this.sleeping = false;
        }

        public bool IsSleeping()
        {
            return this.entity != null ? this.entity.IsSleeping() : this.sleeping;
        }

        private bool nearby;
        private float nearbyDistance;
        private EntityPlayer nearbyPlayer;

        public bool AnyPlayersNearby(out float distance, out EntityPlayer nearby)
        {
            distance = this.nearbyDistance;
            nearby = this.nearbyPlayer;

            return this.nearby;
        }

        public void SetPlayersNearby(ICollection<PlayerSnapshot> nearby)
        {
            if (nearby.Any())
            {
                var nearbyPlayer = nearby.First();
                var nearbyDist = Vector3.Distance(nearbyPlayer.location, this.GetLocation());

                for(int i = 1; i < nearby.Count; i++)
                {
                    var otherPlayer = nearby.ElementAt(i);
                    var otherDist = Vector3.Distance(otherPlayer.location, this.GetLocation());

                    if (otherDist < nearbyDist)
                    {
                        nearbyDist = otherDist;
                        nearbyPlayer = otherPlayer;
                    }
                }

                this.nearbyDistance = nearbyDist;
                this.nearbyPlayer = nearbyPlayer.player;
                this.nearby = true;
            }
            else
            {
                this.nearby = false;
            }
        }

        public bool CanSee(Vector3 pos)
        {
            return this.entity != null ? this.entity.CanSee(pos) : false;
        }

        public bool CanSee(EntityPlayer player)
        {
            return this.entity != null ? this.entity.CanSee(player) : false;
        }

        public void SetTarget(EntityPlayer target)
        {
            if (this.entity == null)
                return;

            this.entity.SetTarget(target);
        }
    }
}
