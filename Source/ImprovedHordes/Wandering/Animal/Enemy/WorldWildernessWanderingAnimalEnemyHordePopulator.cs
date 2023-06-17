using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.POI;

namespace ImprovedHordes.Wandering.Animal.Enemy
{
    public sealed class WorldWildernessWanderingAnimalEnemyHordePopulator : WorldWildernessHordePopulator<WanderingAnimalEnemyHorde>
    {
        public WorldWildernessWanderingAnimalEnemyHordePopulator(float worldSize, WorldPOIScanner scanner, HordeSpawnData spawnData) : base(worldSize, scanner, spawnData, 32)
        {
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator()
        {
            return new WorldWildernessWanderingAnimalEnemyAICommandGenerator();
        }
    }
}
