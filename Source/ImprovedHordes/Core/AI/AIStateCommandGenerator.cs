using ImprovedHordes.Core.Abstractions.World.Random;

namespace ImprovedHordes.Core.AI
{
    public abstract class AIStateCommandGenerator<AIState, CommandType> : IAICommandGenerator<CommandType> where AIState : IAIState where CommandType : AICommand
    {
        private readonly AIState state;

        public AIStateCommandGenerator(AIState initialState)
        {
            this.state = initialState;
        }

        public bool GenerateNextCommand(IWorldRandom worldRandom, out GeneratedAICommand<CommandType> command)
        {
            return GenerateNextCommandFromState(this.state, worldRandom, out command);
        }

        protected abstract bool GenerateNextCommandFromState(AIState state, IWorldRandom worldRandom, out GeneratedAICommand<CommandType> command);
    }
}