using ImprovedHordes.Core.AI;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.AI.Commands
{
    public class GoToTargetAICommand : AICommand
    {
        private const int MIN_DISTANCE_TO_TARGET = 5;
        private Vector3 target;

        public GoToTargetAICommand(Vector3 target) : base()
        {
            this.UpdateTarget(target);
        }

        public GoToTargetAICommand(Vector3 target, double expiryTimeSeconds) : base(expiryTimeSeconds)
        {
            this.UpdateTarget(target);
        }

        public override bool CanExecute(IAIAgent agent)
        {
            return agent.GetTarget() == null || !agent.GetTarget().IsPlayer();
        }

        public override void Execute(IAIAgent agent, float dt)
        {
            agent.MoveTo(this.target, dt);
        }

        public override int GetObjectiveScore(IAIAgent agent)
        {
            return Mathf.FloorToInt(Vector2.Distance(ToXZ(agent.GetLocation()), ToXZ(this.target)));
        }

        public override bool IsComplete(IAIAgent agent)
        {
            return Vector2.Distance(ToXZ(agent.GetLocation()), ToXZ(this.target)) <= MIN_DISTANCE_TO_TARGET;
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