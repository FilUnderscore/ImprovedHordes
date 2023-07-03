using HarmonyLib;
using ImprovedHordes.Core.Abstractions.World;
using System.Reflection;
using UnityEngine;

namespace ImprovedHordes.Implementations.World
{
    public sealed class ImprovedHordesEntity : IEntity
    {
        private static readonly FieldInfo SEE_CACHE_FIELD = AccessTools.DeclaredField(typeof(EntityAlive), "seeCache");

        private readonly EntityAlive entity;

        public ImprovedHordesEntity(EntityAlive entity)
        {
            this.entity = entity;
        }

        public string GetAlertSound()
        {
            return this.entity.GetSoundAlert();
        }

        public int GetEntityClassId()
        {
            return this.entity.entityClass;
        }

        public int GetEntityId()
        {
            return this.entity.entityId;
        }

        public Vector3 GetLocation()
        {
            return this.entity.position;
        }

        public IEntity GetTarget()
        {
            return new ImprovedHordesEntity(this.entity.GetAttackTarget());
        }

        public bool IsDead()
        {
            return this.entity.IsDead();
        }

        public bool IsStunned()
        {
            return this.entity.bodyDamage.CurrentStun != EnumEntityStunType.None;
        }

        public void PlaySound(string soundName)
        {
            this.entity.PlayOneShot(soundName);
        }

        public void MoveTo(Vector3 location, float dt)
        {
            this.entity.SetInvestigatePosition(location, 6000, false);
            AstarManager.Instance.AddLocationLine(this.GetLocation(), location, 64);
        }

        public void Stop()
        {
            if (SEE_CACHE_FIELD == null || SEE_CACHE_FIELD.GetValue(this.entity) == null)
                return;

            this.entity.ClearInvestigatePosition();
        }

        public bool IsMoving()
        {
            return this.entity.HasInvestigatePosition;
        }

        public bool IsPlayer()
        {
            return this.entity is EntityPlayer;
        }

        public void Sleep()
        {
            this.entity.SetSleeper();
            this.entity.TriggerSleeperPose(0);
        }

        public void WakeUp()
        {
            this.entity.ConditionalTriggerSleeperWakeUp();
        }

        public bool IsSleeping()
        {
            return this.entity.IsSleeping;
        }

        public bool AnyPlayersNearby(out float distance, out EntityPlayer nearby)
        {
            distance = 0.0f;
            nearby = null;
            return false;
        }

        public bool CanSee(Vector3 position)
        {
            return this.entity.emodel != null && this.entity.emodel.GetModelTransform() != null && this.entity.CanSee(position);
        }

        public bool CanSee(EntityPlayer player)
        {
            if (SEE_CACHE_FIELD == null || SEE_CACHE_FIELD.GetValue(this.entity) == null)
                return CanSee(player.position);

            return this.entity.emodel != null && this.entity.emodel.GetModelTransform() != null && this.entity.CanSee(player);
        }

        public void SetTarget(EntityPlayer player)
        {
            this.entity.SetAttackTarget(player, 600);
        }
    }
}
