using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;
using UnityEngine;

namespace ImprovedHordes.Wandering.Animal.Enemy
{
    public sealed class WorldWildernessWanderingAnimalEnemyAICommandGenerator : AIStateCommandGenerator<WanderingAnimalAIState, AICommand>
    {
        public WorldWildernessWanderingAnimalEnemyAICommandGenerator() : base(new WanderingAnimalAIState())
        {
        }

        protected override bool GenerateNextCommandFromState(WanderingAnimalAIState state, IWorldRandom worldRandom, out GeneratedAICommand<AICommand> command)
        {
            // TODO: Implement cluster information so we can generate commands based on the world horde state.

            switch(state.GetWanderingState())
            {
                case WanderingAnimalAIState.WanderingState.IDLE:
                    Vector3 targetLocation = worldRandom.RandomLocation3;
                    state.SetTargetLocation(targetLocation);
                    state.SetWanderingState(WanderingAnimalAIState.WanderingState.MOVING);

                    float wanderTime = 100.0f + worldRandom.RandomRange(1000);
                    state.SetRemainingWanderTime(wanderTime);

                    break;
                case WanderingAnimalAIState.WanderingState.WANDER:
                    command = new GeneratedAICommand<AICommand>(new WanderAICommand(state.GetRemainingWanderTime()), (_) =>
                    {
                        // On complete, change to idle.
                        state.SetWanderingState(WanderingAnimalAIState.WanderingState.IDLE);
                    });

                    return true;
                case WanderingAnimalAIState.WanderingState.MOVING:
                    // Continue moving to target.
                    break;
            }

            if(state.GetWanderingState() != WanderingAnimalAIState.WanderingState.WANDER)
            {
                command = new GeneratedAICommand<AICommand>(new GoToTargetAICommand(state.GetTargetLocation()), (_) =>
                {
                    state.SetWanderingState(WanderingAnimalAIState.WanderingState.WANDER);
                });

                return true;
            }

            // This should never be reached.
            command = null;
            return false;
        }
    }
}
