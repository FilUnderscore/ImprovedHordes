using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;
using ImprovedHordes.POI;
using UnityEngine;

namespace ImprovedHordes.Wandering.Enemy.Wilderness
{
    public sealed class WorldWildernessWanderingEnemyAICommandGenerator : AIStateCommandGenerator<WanderingEnemyAIState, AICommand>
    {
        private const float SLEEP_CHANCE = 0.7f;

        private readonly WorldPOIScanner worldPOIScanner;
        public readonly BiomeDefinition Biome;

        public WorldWildernessWanderingEnemyAICommandGenerator(WorldPOIScanner worldPOIScanner, BiomeDefinition biome) : base(new WanderingEnemyAIState())
        {
            this.worldPOIScanner = worldPOIScanner;
            this.Biome = biome;
        }

        protected override bool GenerateNextCommandFromState(WanderingEnemyAIState state, IWorldRandom worldRandom, out GeneratedAICommand<AICommand> command)
        {
            switch(state.GetWanderingState())
            {
                case WanderingEnemyAIState.WanderingState.IDLE:
                    // Set next target zone / location.
                    bool zoneOrWild = worldRandom.RandomChance(0.1f);
                    WorldPOIScanner.POIZone zone;

                    if(zoneOrWild && (zone = worldRandom.Random(this.worldPOIScanner.GetBiomeZones(this.Biome))) != null)
                    {
                        state.SetTargetZone(zone);
                    }
                    else
                    {
                        state.SetTargetLocation(worldRandom.RandomLocation3);
                    }

                    state.SetWanderingState(WanderingEnemyAIState.WanderingState.MOVING);

                    float wanderTime = 100.0f + worldRandom.RandomRange(48) * 100.0f;
                    state.SetRemainingWanderTime(wanderTime);

                    bool sleep = worldRandom.RandomChance(SLEEP_CHANCE);

                    if (sleep)
                    {
                        command = new GeneratedAICommand<AICommand>(new SleepingAICommand(wanderTime));
                        return true;
                    }

                    break;
                case WanderingEnemyAIState.WanderingState.WANDER:
                    command = new GeneratedAICommand<AICommand>(new WanderAICommand(state.GetTargetLocation().Value, worldRandom, 20.0f, state.GetRemainingWanderTime()), (_) =>
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
                    // Continue moving to zone / location.
                    break;
            }

            if(state.GetWanderingState() != WanderingEnemyAIState.WanderingState.WANDER)
            {
                if(state.GetTargetZone() == null && state.GetTargetLocation() == null)
                {
                    state.SetWanderingState(WanderingEnemyAIState.WanderingState.IDLE);

                    command = null;
                    return false;
                }
                else
                {
                    GoToTargetAICommand targetCommand;

                    if(state.GetTargetZone() != null)
                    {
                        var zone = state.GetTargetZone();
                        zone.GetLocationOutside(worldRandom, out Vector2 zoneLocation);
                        targetCommand = new GoToTargetAICommand(zoneLocation);
                        state.SetTargetLocation(zoneLocation);
                    }
                    else if(state.GetTargetLocation() != null)
                    {
                        var location = state.GetTargetLocation().Value;
                        targetCommand = new GoToTargetAICommand(location);
                    }
                    else
                    {
                        // This should never be reached.
                        command = null;
                        return false;
                    }

                    command = new GeneratedAICommand<AICommand>(targetCommand, (_) =>
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
