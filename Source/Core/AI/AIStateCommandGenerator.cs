namespace ImprovedHordes.Core.AI
{
    public abstract class AIStateCommandGenerator<AIState, CommandType> : IAICommandGenerator<CommandType> where AIState : IAIState where CommandType : AICommand
    {
        private readonly AIState state;

        public AIStateCommandGenerator(AIState initialState)
        {
            this.state = initialState;
        }

        public bool GenerateNextCommand(out GeneratedAICommand<CommandType> command)
        {
            return GenerateNextCommandFromState(this.state, out command);
        }

        public abstract bool GenerateNextCommandFromState(AIState state, out GeneratedAICommand<CommandType> command);
    }
}