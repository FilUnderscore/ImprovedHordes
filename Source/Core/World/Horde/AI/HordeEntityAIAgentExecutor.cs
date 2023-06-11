using ImprovedHordes.Core.Abstractions;
using ImprovedHordes.Core.AI;

namespace ImprovedHordes.Core.World.Horde.AI
{
    public sealed class HordeEntityAIAgentExecutor : AIAgentExecutor<IEntity>
    {
        private readonly HordeAIAgentExecutor hordeAIAgentExecutor;
        private bool loaded;

        // Entity commands are separate AICommands than horde commands and run parallel when entities are loaded.
        private GeneratedAICommand<EntityAICommand> entityCommand;
        private readonly IAICommandGenerator<EntityAICommand> entityCommandGenerator;

        public HordeEntityAIAgentExecutor(IEntity agent, HordeAIAgentExecutor hordeAIAgentExecutor, IAICommandGenerator<EntityAICommand> entityCommandGenerator) : base(agent)
        {
            this.hordeAIAgentExecutor = hordeAIAgentExecutor;
            this.entityCommandGenerator = entityCommandGenerator;
        }

        private bool UpdateEntityCommand(float dt)
        {
            if (this.entityCommand == null || this.entityCommand.Command == null || !this.entityCommand.Command.CanExecute(this.agent))
                return false;

            this.entityCommand.Command.Execute(this.agent, dt);

            if (!this.entityCommand.Command.IsComplete(this.agent))
                return true;

#if DEBUG
            Log.Out($"Entity completed EntityAICommand {this.entityCommand.Command.GetType().Name}");
#endif

            if (this.entityCommand.OnComplete != null)
                this.entityCommand.OnComplete.Invoke(this.entityCommand.Command);

            return false;
        }

        public override void Update(float dt)
        {
            if (this.entityCommandGenerator != null)
            {
                if(!this.UpdateEntityCommand(dt)) // Can only be generated.
                {
                    this.entityCommandGenerator.GenerateNextCommand(out this.entityCommand);
                }
            }

            if (this.UpdateCommand(dt))
                return;

            this.command = this.hordeAIAgentExecutor.GetNextCommand(this.command);
        }

        public bool IsLoaded()
        {
            return this.loaded;
        }

        public void SetLoaded(bool loaded)
        {
            this.loaded = loaded;
        }
    }
}
