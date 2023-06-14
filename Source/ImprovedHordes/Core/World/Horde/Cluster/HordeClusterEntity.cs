﻿using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Core.World.Horde.Spawn.Request;
using System;
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

        public void MoveTo(Vector3 location, float dt)
        {
            if(this.spawned)
            {
                this.MoveToSpawned(location);
            }
            else
            {
                this.MoveToDespawned(location, dt);
            }
        }

        private void MoveToSpawned(Vector3 location)
        {
            Vector3 directionWithinLoadDistance = (location - this.GetLocation()).normalized;
            Vector3 locationWithinLoadDistance = (directionWithinLoadDistance * WorldHordeTracker.MAX_VIEW_DISTANCE) + this.GetLocation();

            this.entity.MoveTo(locationWithinLoadDistance, 0);
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

        public void RequestDespawn(ILoggerFactory loggerFactory, MainThreadRequestProcessor mainThreadRequestProcessor, Action<IEntity> onDespawn)
        {
            this.awaitingSpawnStateChange = true;
            mainThreadRequestProcessor.Request(new HordeEntitySpawnRequest(loggerFactory, null, this, false, onDespawn));
        }

        public void RequestSpawn(ILoggerFactory loggerFactory, IEntitySpawner spawner, MainThreadRequestProcessor mainThreadRequestProcessor, Action<IEntity> onSpawn)
        {
            this.awaitingSpawnStateChange = true;
            mainThreadRequestProcessor.Request(new HordeEntitySpawnRequest(loggerFactory, spawner, this, true, onSpawn));
        }

        public void Despawn()
        {
            this.location = this.entity.GetLocation();
            GameManager.Instance.World.RemoveEntity(this.entity.GetEntityId(), EnumRemoveEntityReason.Killed);

            this.entity = null;
            this.spawned = false;
            this.awaitingSpawnStateChange = false;
        }

        public void Respawn(Abstractions.Logging.ILogger logger, IEntitySpawner spawner)
        {
            float surfaceSpawnHeight = GameManager.Instance.World.GetHeightAt(this.location.x, this.location.z) + 1.0f;
            this.location.y = surfaceSpawnHeight;

            if(!GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(this.location, 0, 10, -1, false, out Vector3 spawnLocation, false))
            {
                logger.Warn($"Failed to respawn HordeClusterEntity at {this.location}");
                return;
            }

            this.entity = spawner.SpawnAt(this.entityClassId, this.entityId, spawnLocation);
            this.entityId = this.entity.GetEntityId();
            this.location = spawnLocation;
            this.spawned = true;
            
            if (this.hordeDespawned)
                this.Despawn();
            else
                this.awaitingSpawnStateChange = false;
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
    }
}
