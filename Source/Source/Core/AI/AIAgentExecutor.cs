using ImprovedHordes.Source.Horde.AI;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster.AI
{
    public abstract class AIAgentExecutor
    {
        protected readonly IAIAgent agent;
        protected AICommand command;

        public AIAgentExecutor(IAIAgent agent)
        {
            this.agent = agent;
        }

        public virtual void Update(float dt)
        {
            this.UpdateCommand(dt);
        }

        protected bool UpdateCommand(float dt)
        {
            if (command == null || !command.CanExecute(this.agent))
                return false;

            command.Execute(this.agent, dt);

            if (!command.IsComplete(this.agent))
                return true;

#if DEBUG
            Log.Out($"Agent completed command {command.GetType().Name}");
#endif

            return false;
        }

        public void SetCommand(AICommand command)
        {
            this.command = command;
        }

        public virtual AICommand GetCommand()
        {
            return this.command;
        }

        public IAIAgent GetAgent()
        {
            return this.agent;
        }
    }
}
