using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;

namespace ImprovedHordes.POI
{
    public sealed class GoToWorldZoneAICommandGenerator : IAICommandGenerator<AICommand>
    {
        private readonly WorldPOIScanner scanner;
        private readonly GameRandom random;

        public GoToWorldZoneAICommandGenerator(WorldPOIScanner scanner)
        {
            this.scanner = scanner;
            this.random = GameManager.Instance.World.GetGameRandom();
        }

        public bool GenerateNextCommand(out GeneratedAICommand<AICommand> command)
        {
            //command = new GoToTargetAICommand(scanner.PickRandomZone().GetBounds().center);
            var zones = this.scanner.GetZones();
            var zoneTargetCommand = new GoToTargetAICommand(zones[random.RandomRange(zones.Count)].GetBounds().center);

            command = new GeneratedAICommand<AICommand>(zoneTargetCommand, (c) =>
            {

            });

            return true;
        }
    }
}
