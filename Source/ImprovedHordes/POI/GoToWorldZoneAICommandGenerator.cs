using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;

namespace ImprovedHordes.POI
{
    public sealed class GoToWorldZoneAICommandGenerator : IAICommandGenerator<AICommand>
    {
        private readonly WorldPOIScanner scanner;
        private readonly BiomeDefinition biome;

        public GoToWorldZoneAICommandGenerator(WorldPOIScanner scanner, BiomeDefinition biome)
        {
            this.scanner = scanner;
            this.biome = biome;
        }

        public bool GenerateNextCommand(IWorldRandom worldRandom, out GeneratedAICommand<AICommand> command)
        {
            var zones = this.scanner.GetBiomeZones(this.biome);
            var zoneTargetCommand = new GoToTargetAICommand(worldRandom.Random(zones).GetBounds().center);

            command = new GeneratedAICommand<AICommand>(zoneTargetCommand, (c) =>
            {

            });

            return true;
        }
    }
}
