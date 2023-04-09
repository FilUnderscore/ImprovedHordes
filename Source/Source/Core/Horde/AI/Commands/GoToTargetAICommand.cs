using UnityEngine;

namespace ImprovedHordes.Source.Horde.AI.Commands
{
    public sealed class GoToTargetAICommand : IAICommand
    {
        private const int MIN_DISTANCE_TO_TARGET = 10;
        private Vector3 target;

        public GoToTargetAICommand(Vector3 target)
        {
            this.target = target;
        }

        public bool CanExecute(IAIAgent agent)
        {
            return agent.GetTarget() == null || !(agent.GetTarget() is EntityPlayer);
        }

        public void Execute(IAIAgent agent, float dt)
        {
            agent.MoveTo(this.target, dt);
        }

        public bool IsComplete(IAIAgent agent)
        {
            return Vector3.Distance(agent.GetLocation(), this.target) < MIN_DISTANCE_TO_TARGET;
        }
    }
}