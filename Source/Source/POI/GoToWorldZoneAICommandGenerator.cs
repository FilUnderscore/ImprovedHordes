using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;
using ImprovedHordes.Source.Horde.AI;
using ImprovedHordes.Source.Horde.AI.Commands;

namespace ImprovedHordes.Source.POI
{
    public sealed class GoToWorldZoneAICommandGenerator : IAICommandGenerator
    {
        private WorldPOIScanner scanner;
        private GameRandom random;

        public GoToWorldZoneAICommandGenerator(WorldPOIScanner scanner)
        {
            this.scanner = scanner;
            this.random = GameManager.Instance.World.GetGameRandom();
        }

        public bool GenerateNextCommand(out AICommand command)
        {
            //command = new GoToTargetAICommand(scanner.PickRandomZone().GetBounds().center);
            var zones = this.scanner.GetZones();
            command = new GoToTargetAICommand(zones[random.RandomRange(zones.Count)].GetBounds().center);

            return true;
        }
    }
}
