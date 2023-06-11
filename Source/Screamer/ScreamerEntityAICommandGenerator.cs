using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Event;
using ImprovedHordes.Screamer.Commands;

namespace ImprovedHordes.Screamer
{
    public sealed class ScreamerEntityAICommandGenerator : IAICommandGenerator<EntityAICommand>
    {
        private readonly WorldEventReporter worldEventReporter;

        public ScreamerEntityAICommandGenerator(WorldEventReporter worldEventReporter) 
        {
            this.worldEventReporter = worldEventReporter;
        }

        public bool GenerateNextCommand(out GeneratedAICommand<EntityAICommand> command)
        {
            command = new GeneratedAICommand<EntityAICommand>(new ScreamerEntityAICommand(this.worldEventReporter));
            return true;
        }
    }
}
