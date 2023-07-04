using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Abstractions.World.Random;
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

        public HordeEntityAIAgentExecutor(IEntity agent, IWorldRandom random, HordeAIAgentExecutor hordeAIAgentExecutor, IAICommandGenerator<EntityAICommand> entityCommandGenerator) : base(agent, random)
        {
            this.hordeAIAgentExecutor = hordeAIAgentExecutor;
            this.entityCommandGenerator = entityCommandGenerator;
        }

        private bool UpdateEntityCommand(float dt)
        {
            if (this.entityCommand == null || this.entityCommand.Command == null || !this.entityCommand.Command.CanExecute(this.Agent))
                return false;

            this.entityCommand.Command.Execute(this.Agent, dt);

            if (!this.entityCommand.Command.IsComplete(this.Agent))
                return true;

#if DEBUG
            Log.Out($"Entity completed EntityAICommand {this.entityCommand.Command.GetType().Name}");
#endif

            if (this.entityCommand.OnComplete != null)
                this.entityCommand.OnComplete.Invoke(this.entityCommand.Command);

            return false;
        }

        private bool CanSee(EntityPlayer player)
        {
            return this.Agent.CanSee(player);
        }

        public override void Update(float dt)
        {
            if(this.Agent.AnyPlayersNearby(out float distance, out EntityPlayer nearby) && (CanSee(nearby) || (distance <= 10.0f && nearby.Stealth.ValuePercentUI >= 0.85f)))
            {
                this.Agent.SetTarget(nearby);
            }

            if (this.entityCommandGenerator != null)
            {
                if(!this.UpdateEntityCommand(dt)) // Can only be generated.
                {
                    this.entityCommandGenerator.GenerateNextCommand(this.Random, out this.entityCommand);
                }
            }

            if (this.UpdateCommand(dt))
                return;

            this.Command = this.hordeAIAgentExecutor.GetNextCommand(this.Random, this.Command);
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
