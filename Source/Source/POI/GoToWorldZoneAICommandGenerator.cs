using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;
using ImprovedHordes.Source.Horde.AI;
using ImprovedHordes.Source.Horde.AI.Commands;

namespace ImprovedHordes.Source.POI
{
    public sealed class GoToWorldZoneAICommandGenerator : IAICommandGenerator
    {
        private WorldPOIScanner scanner;

        public GoToWorldZoneAICommandGenerator(WorldPOIScanner scanner)
        {
            this.scanner = scanner;
        }

        public bool GenerateNextCommand(out AICommand command)
        {
            command = new GoToTargetAICommand(scanner.PickRandomZone().GetBounds().center);
            return true;
        }
    }
}
