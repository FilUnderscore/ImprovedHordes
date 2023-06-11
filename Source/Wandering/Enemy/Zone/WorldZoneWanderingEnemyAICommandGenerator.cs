using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;
using ImprovedHordes.POI;
using ImprovedHordes.Source.Wandering.Enemy;

namespace ImprovedHordes.Wandering.Enemy.Zone
{
    public class WorldZoneWanderingEnemyAICommandGenerator : AIStateCommandGenerator<WanderingEnemyAIState>
    {
        private readonly WorldPOIScanner scanner;
        private readonly GameRandom random;

        public WorldZoneWanderingEnemyAICommandGenerator(WorldPOIScanner scanner) : base(new WanderingEnemyAIState())
        {
            this.scanner = scanner;
            this.random = GameManager.Instance.World.GetGameRandom();
        }

        public override bool GenerateNextCommandFromState(WanderingEnemyAIState state, out GeneratedAICommand command)
        {
            switch (state.GetWanderingState())
            {
                case WanderingEnemyAIState.WanderingState.IDLE:
                    var zones = this.scanner.GetZones();
                    var zone = zones[random.RandomRange(zones.Count)];

                    state.SetTargetZone(zone);
                    state.SetWanderingState(WanderingEnemyAIState.WanderingState.MOVING);

                    // Set wander time once per zone.
                    float wanderTime = 100.0f + state.GetTargetZone().GetCount() * 2.0f;
                    state.SetRemainingWanderTime(wanderTime);

                    break;
                case WanderingEnemyAIState.WanderingState.WANDER:
                    // TODO wandering command. On interrupt, change to moving. On complete, change to idle.

                    command = new GeneratedAICommand(new WanderAICommand(state.GetRemainingWanderTime()), (_) =>
                    {
                        // On complete, change to idle.
                        state.SetWanderingState(WanderingEnemyAIState.WanderingState.IDLE);
                    }, (wanderCommand) =>
                    {
                        // On interrupt, change to moving.
                        state.SetRemainingWanderTime(((WanderAICommand)wanderCommand).GetWanderTime());
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

                    command = new GeneratedAICommand(zoneTargetCommand, (_) =>
                    {
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
