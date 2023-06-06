using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde.AI;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class HordeClusterEntity : IAIAgent
    {
        private readonly HordeCluster cluster;
        private EntityAlive entity;

        private readonly int entityClassId;
        private bool spawned, awaitingSpawnStateChange, hordeDespawned;
        private Vector3 location;

        private readonly HordeCharacteristics characteristics;

        public HordeClusterEntity(HordeCluster cluster, EntityAlive entity, HordeCharacteristics characteristics)
        {
            this.cluster = cluster;
            this.entity = entity;

            this.spawned = true;
            this.entityClassId = entity.entityClass;
            this.location = entity.position;

            this.characteristics = characteristics;
        }

        public HordeCluster GetCluster()
        {
            return this.cluster;
        }

        public EntityAlive GetEntity()
        {
            return this.entity;
        }

        public bool CanInterrupt()
        {
            return this.GetTarget() == null || !(this.GetTarget() is EntityPlayer);
        }

        public Vector3 GetLocation()
        {
            return this.spawned ? this.entity.position : this.location;
        }

        public EntityAlive GetTarget()
        {
            return this.spawned ? this.entity.GetAttackTarget() : null;
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

            this.entity.SetInvestigatePosition(locationWithinLoadDistance, 6000, false);
            AstarManager.Instance.AddLocationLine(this.GetLocation(), locationWithinLoadDistance, 64);
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

        public void RequestDespawn(MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            mainThreadRequestProcessor.Request(new HordeEntitySpawnRequest(this, false));
            this.awaitingSpawnStateChange = true;
        }

        public void RequestSpawn(MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            mainThreadRequestProcessor.Request(new HordeEntitySpawnRequest(this, true));
            this.awaitingSpawnStateChange = true;
        }

        public void Despawn()
        {
            this.location = this.entity.position;
            GameManager.Instance.World.RemoveEntity(this.entity.entityId, EnumRemoveEntityReason.Killed);

            this.entity = null;
            this.spawned = false;
            this.awaitingSpawnStateChange = false;
        }

        public void Respawn()
        {
            float surfaceSpawnHeight = GameManager.Instance.World.GetHeightAt(this.location.x, this.location.z) + 1.0f;
            this.location.y = surfaceSpawnHeight;

            if(!GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(this.location, 0, 10, -1, false, out Vector3 spawnLocation, false))
            {
                Log.Warning($"Failed to respawn HordeClusterEntity at {this.location}");
                return;
            }

            this.entity = HordeEntityGenerator.GenerateEntity(this.entityClassId, spawnLocation);
            this.location = spawnLocation;
            this.spawned = true;
            this.awaitingSpawnStateChange = false;

            if (this.hordeDespawned)
                this.Despawn();
        }

        public void NotifyHordeDespawned()
        {
            this.hordeDespawned = true;
        }
    }
}
