using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;
using ImprovedHordes.POI;
using ImprovedHordes.Screamer.Commands;

namespace ImprovedHordes.Screamer
{
    public sealed class WorldZoneScreamerAICommandGenerator : AIStateCommandGenerator<ScreamerAIState, AICommand>
    {
        private const float ZONE_WANDER_RANGE_MULTIPLIER = 0.25f;

        public WorldZoneScreamerAICommandGenerator(WorldPOIScanner.POIZone zone) : base(new ScreamerAIState(zone))
        {
        }

        protected override bool GenerateNextCommandFromState(ScreamerAIState state, IWorldRandom worldRandom, out GeneratedAICommand<AICommand> command)
        {
            switch(state.GetWanderState())
            {
                case ScreamerAIState.WanderState.IDLE:
                    // Set next wander location within zone.
                    state.SetWanderState(ScreamerAIState.WanderState.MOVING);

                    float wanderTime = 100.0f + state.GetPOIZone().GetCount() * 2.0f + worldRandom.RandomRange(10) * 100.0f;
                    state.SetRemainingWanderTime(wanderTime);

                    break;
                case ScreamerAIState.WanderState.WANDERING:
                    command = new GeneratedAICommand<AICommand>(new WanderAICommand(state.GetRemainingWanderTime()), (_) =>
                    {
                        // On complete, change to idle.
                        state.SetWanderState(ScreamerAIState.WanderState.IDLE);
                    }, (wanderCommand) =>
                    {
                        // On interrupt, change to moving.
                        state.SetRemainingWanderTime(((WanderAICommand)wanderCommand).GetWanderTime());
                        state.SetWanderState(ScreamerAIState.WanderState.MOVING);
                    });

                    return true;
                case ScreamerAIState.WanderState.MOVING:
                    // Continue moving to next wander location within zone.
                    break;
            }

            if(state.GetWanderState() != ScreamerAIState.WanderState.WANDERING)
            {
                command = new GeneratedAICommand<AICommand>(new ZoneWanderAICommand(state.GetPOIZone(), worldRandom, false), (_) =>
                {
                    state.SetWanderState(ScreamerAIState.WanderState.WANDERING);
                });

                return true;
            }

            // This should never be reached.
            command = null;
            return false;
        }
    }
}
