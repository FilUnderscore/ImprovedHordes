using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.POI;

namespace ImprovedHordes.Wandering.Animal.Enemy
{
    public sealed class WorldWildernessWanderingAnimalEnemyHordePopulator : WorldWildernessHordePopulator<WanderingAnimalEnemyHorde>
    {
        private static readonly Setting<int> WANDERING_ANIMAL_ENEMY_WILDERNESS_SPARSITY = new Setting<int>("wandering_animal_enemy_wilderness_sparsity", 32);

        public WorldWildernessWanderingAnimalEnemyHordePopulator(float worldSize, WorldPOIScanner scanner, HordeSpawnParams spawnData) : base(worldSize, scanner, spawnData, WANDERING_ANIMAL_ENEMY_WILDERNESS_SPARSITY.Value, true)
        {
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator()
        {
            return new WorldWildernessWanderingAnimalEnemyAICommandGenerator();
        }
    }
}
