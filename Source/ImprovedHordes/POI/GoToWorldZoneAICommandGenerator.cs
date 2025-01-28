using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.AI.Commands;

namespace ImprovedHordes.POI
{
    public sealed class GoToWorldZoneAICommandGenerator : IAICommandGenerator<AICommand>
    {
        private readonly WorldPOITracker tracker;
        private readonly BiomeDefinition biome;

        public GoToWorldZoneAICommandGenerator(WorldPOITracker tracker, BiomeDefinition biome)
        {
            this.tracker = tracker;
            this.biome = biome;
        }

        public bool GenerateNextCommand(IWorldRandom worldRandom, out GeneratedAICommand<AICommand> command)
        {
            var poiTarget = this.tracker.GetRandomPOI(worldRandom, poi => HordeBiomes.GetBiomeAt(poi.GetLocation(), true) == this.biome);
            var poiTargetCommand = new GoToTargetAICommand(poiTarget.GetLocation());

            command = new GeneratedAICommand<AICommand>(poiTargetCommand, (c) =>
            {

            });

            return true;
        }
    }
}
