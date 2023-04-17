using ImprovedHordes.Source.Horde.AI;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class HordeEntity : IAIAgent
    {
        private readonly HordeCluster cluster;
        private readonly EntityAlive entity;
        
        public HordeEntity(HordeCluster cluster, EntityAlive entity) 
        {
            this.cluster = cluster;
            this.entity = entity;
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
            return this.entity.position;
        }

        public EntityAlive GetTarget()
        {
            return this.entity.GetAttackTarget();
        }

        public bool IsDead()
        {
            return this.entity.IsDead();
        }

        public void MoveTo(Vector3 location, float dt)
        {
            this.entity.SetInvestigatePosition(location, 6000, false);
            AstarManager.Instance.AddLocationLine(this.GetLocation(), location, 64);
        }
    }
}
