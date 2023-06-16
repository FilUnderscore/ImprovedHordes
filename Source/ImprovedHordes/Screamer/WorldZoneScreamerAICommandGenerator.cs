using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.POI;
using ImprovedHordes.Screamer.Commands;

namespace ImprovedHordes.Screamer
{
    public sealed class WorldZoneScreamerAICommandGenerator : AIStateCommandGenerator<ScreamerAIState, AICommand>
    {
        public WorldZoneScreamerAICommandGenerator(WorldPOIScanner.POIZone zone) : base(new ScreamerAIState(zone))
        {
        }

        protected override bool GenerateNextCommandFromState(ScreamerAIState state, IWorldRandom worldRandom, out GeneratedAICommand<AICommand> command)
        {
            command = new GeneratedAICommand<AICommand>(new ZoneWanderAICommand(state.GetPOIZone()));
            return true;
        }
    }
}
