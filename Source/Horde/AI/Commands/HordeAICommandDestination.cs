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

            if (Vector3.Distance(entityPosition, this.targetPosition) <= this.distanceTolerance)
                return true;

            return false;
        }

        public override void Execute(double _, EntityAlive alive)
        {
            alive.SetInvestigatePosition(this.targetPosition, 6000, false);
        }
    }
}
