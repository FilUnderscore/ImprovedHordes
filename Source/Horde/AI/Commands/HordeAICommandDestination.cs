using UnityEngine;

using static ImprovedHordes.IHLog;

namespace ImprovedHordes.Horde.AI.Commands
{
    public class HordeAICommandDestination : HordeAICommand
    {
        protected Vector3 targetPosition;

        protected int distanceTolerance;

        public HordeAICommandDestination(Vector3 target, int distanceTolerance)
        {
            this.targetPosition = target;
            this.distanceTolerance = distanceTolerance;
        }

        public override bool CanExecute(EntityAlive alive)
        {
            bool attacking = alive.GetAttackTarget() != null;
            bool investigatingSomethingElse = alive.HasInvestigatePosition && alive.InvestigatePosition != this.targetPosition;

            return !attacking && !investigatingSomethingElse;
        }

        public override bool IsFinished(EntityAlive alive)
        {
            Vector3 entityPosition = alive.position;
            
            if (Vector2.Distance(ToXZ(entityPosition), ToXZ(targetPosition)) <= this.distanceTolerance)
                return true;

            return false;
        }

        public override void Execute(double _, EntityAlive alive)
        {
            alive.SetInvestigatePosition(this.targetPosition, 6000, false);
        }

        private Vector2 ToXZ(Vector3 vec3)
        {
            return new Vector2(vec3.x, vec3.z);
        }
    }
}
