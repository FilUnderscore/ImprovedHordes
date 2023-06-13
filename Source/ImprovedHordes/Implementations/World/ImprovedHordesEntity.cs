using ImprovedHordes.Core.Abstractions.World;
using UnityEngine;

namespace ImprovedHordes.Implementations.World
{
    public sealed class ImprovedHordesEntity : IEntity
    {
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
    }
}
