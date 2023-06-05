using ImprovedHordes.Source.Horde.AI;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class HordeClusterEntity : IAIAgent
    {
        private readonly HordeCluster cluster;
        private readonly EntityAlive entity;
        
        public HordeClusterEntity(HordeCluster cluster, EntityAlive entity) 
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
            Vector3 directionWithinLoadDistance = (location - this.GetLocation()).normalized;
            Vector3 locationWithinLoadDistance = (directionWithinLoadDistance * WorldHordeTracker.MAX_VIEW_DISTANCE) + this.GetLocation();

            this.entity.SetInvestigatePosition(locationWithinLoadDistance, 6000, false);
            AstarManager.Instance.AddLocationLine(this.GetLocation(), locationWithinLoadDistance, 64);
        }
    }
}
