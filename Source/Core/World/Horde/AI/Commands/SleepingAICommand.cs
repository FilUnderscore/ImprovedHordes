using ImprovedHordes.Core.AI;

namespace ImprovedHordes.Core.World.Horde.AI.Commands
{
    public sealed class SleepingAICommand : AICommand
    {
        public override bool CanExecute(IAIAgent agent)
        {
            return true;
        }

        public override void Execute(IAIAgent agent, float dt)
        {
            if (agent.IsSleeping())
                return;

            agent.Sleep();
        }

        public override int GetObjectiveScore(IAIAgent agent)
        {
            return 0;
        }

        public override bool IsComplete(IAIAgent agent)
        {
            return agent.GetTarget() != null;
        }
    }
}
