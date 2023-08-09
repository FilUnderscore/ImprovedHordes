using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;
using ImprovedHordes.POI;
using ImprovedHordes.Screamer.Commands;
using UnityEngine;

namespace ImprovedHordes.Wandering.Enemy.Zone
{
    public sealed class WorldZoneWanderingEnemyAICommandGenerator : AIStateCommandGenerator<WanderingEnemyAIState, AICommand>
    {
        private readonly WorldPOIScanner scanner;
        private readonly BiomeDefinition biome;

        public WorldZoneWanderingEnemyAICommandGenerator(WorldPOIScanner scanner, WorldPOIScanner.POIZone zone) : base(new WanderingEnemyAIState(zone))
        {
            this.scanner = scanner;
            this.biome = zone.GetBiome();
        }

        protected override bool GenerateNextCommandFromState(WanderingEnemyAIState state, IWorldRandom worldRandom, out GeneratedAICommand<AICommand> command)
        {
            switch (state.GetWanderingState())
            {
                case WanderingEnemyAIState.WanderingState.IDLE:
                    // Set next zone target and begin moving.
                    var zones = this.scanner.GetBiomeZones(this.biome);
                    var zone = worldRandom.Random(zones);

                    state.SetTargetZone(zone);
                    state.SetWanderingState(WanderingEnemyAIState.WanderingState.MOVING);

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
                    zone.GetLocationOutside(worldRandom, out Vector2 location);

                    command = new GeneratedAICommand<AICommand>(new GoToTargetAICommand(location, true, false), (_) =>
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
