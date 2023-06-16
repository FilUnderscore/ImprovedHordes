using ImprovedHordes.Core.AI;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.AI.Commands
{
    public class GoToTargetAICommand : AICommand
    {
        private const int MIN_DISTANCE_TO_TARGET = 10;
        private Vector3 target;

        public GoToTargetAICommand(Vector3 target) : base()
        {
            this.target = target;
        }

        public GoToTargetAICommand(Vector3 target, double expiryTimeSeconds) : base(expiryTimeSeconds)
        {
            this.target = target;
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
            return Vector2.Distance(ToXZ(agent.GetLocation()), ToXZ(this.target)) < MIN_DISTANCE_TO_TARGET;
        }

        private Vector2 ToXZ(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }
    }
}