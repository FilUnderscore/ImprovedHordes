using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.POI;

namespace ImprovedHordes.Wandering.Animal
{
    public sealed class WorldWildernessWanderingAnimalHordePopulator : WorldWildernessHordePopulator<WanderingAnimalHorde>
    {
        private static readonly Setting<int> WANDERING_ANIMAL_WILDERNESS_SPARSITY = new Setting<int>("wandering_animal_wilderness_sparsity", 64);

        public WorldWildernessWanderingAnimalHordePopulator(float worldSize, WorldPOIScanner scanner, HordeSpawnParams spawnData) : base(worldSize, scanner, spawnData, WANDERING_ANIMAL_WILDERNESS_SPARSITY.Value, false)
        {
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(BiomeDefinition biome)
        {
            return new WorldWildernessWanderingAnimalAICommandGenerator();
        }
    }
}
