using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;

namespace ImprovedHordes.Source.Core.AI
{
    public abstract class AIStateCommandGenerator<AIState> : IAICommandGenerator where AIState : IAIState
    {
        private readonly AIState state;

        public AIStateCommandGenerator(AIState initialState)
        {
            this.state = initialState;
        }

        public bool GenerateNextCommand(out GeneratedAICommand command)
        {
            return GenerateNextCommandFromState(this.state, out command);
        }

        public abstract bool GenerateNextCommandFromState(AIState state, out GeneratedAICommand command);
    }
}