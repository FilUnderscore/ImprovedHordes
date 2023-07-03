using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;
using ImprovedHordes.POI;
using ImprovedHordes.Screamer.Commands;

namespace ImprovedHordes.Wandering.Enemy.Zone
{
    public sealed class WorldZoneWanderingEnemyAICommandGenerator : AIStateCommandGenerator<WanderingEnemyAIState, AICommand>
    {
        private const float SLEEP_CHANCE = 0.3f;
        private readonly WorldPOIScanner scanner;
        
        public WorldZoneWanderingEnemyAICommandGenerator(WorldPOIScanner scanner, WorldPOIScanner.POIZone zone) : base(new WanderingEnemyAIState(zone))
        {
            this.scanner = scanner;
        }

        protected override bool GenerateNextCommandFromState(WanderingEnemyAIState state, IWorldRandom worldRandom, out GeneratedAICommand<AICommand> command)
        {
            switch (state.GetWanderingState())
            {
                case WanderingEnemyAIState.WanderingState.IDLE:
                    // Set next zone target and begin moving.
                    var zones = this.scanner.GetZones();
                    var zone = worldRandom.Random(zones);

                    state.SetTargetZone(zone);
                    state.SetWanderingState(WanderingEnemyAIState.WanderingState.MOVING);

                    bool sleep = worldRandom.RandomChance(SLEEP_CHANCE);

                    if (sleep)
                    {
                        float sleepTime = 100.0f + state.GetTargetZone().GetCount() * 2.0f + worldRandom.RandomRange(48) * 100.0f;

                        command = new GeneratedAICommand<AICommand>(new SleepingAICommand(sleepTime));
                        return true;
                    }

                    break;
                case WanderingEnemyAIState.WanderingState.WANDER:
                    command = new GeneratedAICommand<AICommand>(new ZoneWanderAICommand(state.GetTargetZone(), worldRandom, true), (_) =>
                    {
                        // On complete, change to idle.
                        state.SetWanderingState(WanderingEnemyAIState.WanderingState.IDLE);
                    }, (wanderCommand) =>
                    {
                        // On interrupt, change to moving.
                        state.SetWanderingState(WanderingEnemyAIState.WanderingState.MOVING);
                    });

                    return true;
                case WanderingEnemyAIState.WanderingState.MOVING:
                    // Continue moving to zone.
                    break;
            }

            if(state.GetWanderingState() != WanderingEnemyAIState.WanderingState.WANDER)
            {
                if (state.GetTargetZone() == null)
                {
                    state.SetWanderingState(WanderingEnemyAIState.WanderingState.IDLE);

                    command = null;
                    return false;
                }
                else
                {
                    var zone = state.GetTargetZone();
                    var zoneTargetCommand = new GoToTargetAICommand(zone.GetBounds().center);

                    command = new GeneratedAICommand<AICommand>(zoneTargetCommand, (_) =>
                    {
                        // On complete, change to wander.
                        state.SetWanderingState(WanderingEnemyAIState.WanderingState.WANDER);
                    });

                    return true;
                }
            }

            // This should never be reached.
            command = null;
            return false;
        }
    }
}
