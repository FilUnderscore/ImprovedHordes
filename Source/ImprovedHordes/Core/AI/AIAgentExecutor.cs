using ImprovedHordes.Core.Abstractions.World.Random;

namespace ImprovedHordes.Core.AI
{
    public abstract class AIAgentExecutor<AgentType> where AgentType : IAIAgent
    {
        protected readonly AgentType Agent;
        protected readonly IWorldRandom Random;
        protected GeneratedAICommand<AICommand> Command;

        public AIAgentExecutor(AgentType agent, IWorldRandom worldRandom)
        {
            this.Agent = agent;
            this.Random = worldRandom;
        }

        public virtual void Update(float dt)
        {
            this.UpdateCommand(dt);
        }

        protected bool UpdateCommand(float dt)
        {
            if (Command == null || Command.Command == null || !Command.Command.CanExecute(this.Agent))
                return false;

            this.Command.Command.Execute(this.Agent, dt);

            if (!this.Command.Command.IsComplete(this.Agent))
                return true;

#if DEBUG
            Log.Out($"{typeof(AgentType).Name} Agent completed command {Command.Command.GetType().Name}");
#endif

            this.Command.Command.OnCompleted(this.Agent);

            if(this.Command.OnComplete != null)
                this.Command.OnComplete.Invoke(this.Command.Command);

            return false;
        }

        public void SetCommand(GeneratedAICommand<AICommand> command)
        {
            if (this.Command != null)
            {
                this.Command.Command.OnInterrupted(this.Agent);

                if(this.Command.OnInterrupt != null)
                    this.Command.OnInterrupt.Invoke(this.Command.Command);
            }

            this.Command = command;
        }

        public virtual GeneratedAICommand<AICommand> GetCommand()
        {
            return this.Command;
        }

        public IAIAgent GetAgent()
        {
            return this.Agent;
        }
    }
}
