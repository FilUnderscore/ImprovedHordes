using ImprovedHordes.Core.AI;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.AI.Commands
{
    public class GoToTargetAICommand : AICommand
    {
        private const int MIN_DISTANCE_TO_TARGET = 10;
        private Vector3 target;
        private bool canRun, canBreak;

        public GoToTargetAICommand(Vector2 target, bool canRun = false, bool canBreak = false) : this(new Vector3(target.x, target.y), canRun, canBreak) {}

        public GoToTargetAICommand(Vector3 target, bool canRun = false, bool canBreak = false) : base()
        {
            this.UpdateTarget(target);
            this.canRun = canRun;
            this.canBreak = canBreak;
        }

        public GoToTargetAICommand(Vector3 target, bool canRun, bool canBreak, double expiryTimeSeconds) : base(expiryTimeSeconds)
        {
            this.UpdateTarget(target);
            this.canRun = canRun;
            this.canBreak = canBreak;
        }

        public override bool CanExecute(IAIAgent agent)
        {
            return agent.GetTarget() == null || !agent.GetTarget().IsPlayer();
        }

        public override void Execute(IAIAgent agent, float dt)
        {
            if (agent.IsMoving())
                return;

            agent.MoveTo(this.target, this.canRun, this.canBreak, dt);
        }

        public override int GetObjectiveScore(IAIAgent agent)
        {
            return Mathf.FloorToInt(Vector2.Distance(ToXZ(agent.GetLocation()), ToXZ(this.target)));
        }

        public override bool IsComplete(IAIAgent agent)
        {
            return Vector2.Distance(ToXZ(agent.GetLocation()), ToXZ(this.target)) <= MIN_DISTANCE_TO_TARGET;
        }

        public override void OnCompleted(IAIAgent agent)
        {
            if (!agent.IsMoving())
                return;

            agent.Stop();
        }

        public override void OnInterrupted(IAIAgent agent)
        {
            if (!agent.IsMoving())
                return;

            agent.Stop();
        }

        private static Vector3 ToGround(Vector3 v)
        {
            float y = GameManager.Instance.World.GetHeightAt(v.x, v.z) + 1.0f;
            return new Vector3(v.x, y, v.z);
        }

        private Vector2 ToXZ(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        protected void UpdateTarget(Vector3 target)
        {
            this.target = ToGround(target);
        }
    }
}