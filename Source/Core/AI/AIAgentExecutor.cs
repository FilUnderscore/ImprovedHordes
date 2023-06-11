namespace ImprovedHordes.Core.AI
{
    public abstract class AIAgentExecutor<AgentType> where AgentType : IAIAgent
    {
        protected readonly AgentType agent;
        protected GeneratedAICommand<AICommand> command;

        public AIAgentExecutor(AgentType agent)
        {
            this.agent = agent;
        }

        public virtual void Update(float dt)
        {
            this.UpdateCommand(dt);
        }

        protected bool UpdateCommand(float dt)
        {
            if (command == null || command.Command == null || !command.Command.CanExecute(this.agent))
                return false;

            this.command.Command.Execute(this.agent, dt);

            if (!this.command.Command.IsComplete(this.agent))
                return true;

#if DEBUG
            Log.Out($"Agent completed command {command.Command.GetType().Name}");
#endif

            if(this.command.OnComplete != null)
                this.command.OnComplete.Invoke(this.command.Command);

            return false;
        }

        public void SetCommand(GeneratedAICommand<AICommand> command)
        {
            if(this.command != null && this.command.OnInterrupt != null)
                this.command.OnInterrupt.Invoke(this.command.Command);

            this.command = command;
        }

        public virtual GeneratedAICommand<AICommand> GetCommand()
        {
            return this.command;
        }

        public IAIAgent GetAgent()
        {
            return this.agent;
        }
    }
}
