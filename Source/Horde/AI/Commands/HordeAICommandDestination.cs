using UnityEngine;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.AI.Commands
{
    public class HordeAICommandDestination : HordeAICommand
    {
        protected Vector3 targetPosition = Vector3.zero;

        protected int distanceTolerance;

        public HordeAICommandDestination(Vector3 target, int distanceTolerance)
        {
            this.RandomizeTargetPosition(target, distanceTolerance);
            this.distanceTolerance = distanceTolerance;
        }

        private void RandomizeTargetPosition(Vector3 target, int distanceTolerance)
        {
            GameRandom random = ImprovedHordesManager.Instance.Random;

            Vector2 pos = random.RandomInsideUnitCircle * distanceTolerance;

            this.targetPosition = new Vector3(pos.x, 0, pos.y) + target;
            Utils.GetSpawnableY(ref this.targetPosition);
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

            if (CanExecute(alive) && Vector2.Distance(ToXZ(entityPosition), ToXZ(targetPosition)) <= this.distanceTolerance)
            {
                alive.ClearInvestigatePosition(); // Clear investigate so they don't keep walking into each other.

                return true;
            }

            return false;
        }

        public override void Execute(float _, EntityAlive alive)
        {
            alive.SetInvestigatePosition(this.targetPosition, 6000, false);

            AstarManager.Instance.AddLocationLine(alive.position, this.targetPosition, 64);
        }

        private Vector2 ToXZ(Vector3 vec3)
        {
            return new Vector2(vec3.x, vec3.z);
        }
    }
}
