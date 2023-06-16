using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.POI;

namespace ImprovedHordes.Wandering.Animal
{
    public sealed class WorldWildernessWanderingAnimalHordePopulator : WorldWildernessHordePopulator<WanderingAnimalHorde>
    {
        public WorldWildernessWanderingAnimalHordePopulator(float worldSize, WorldPOIScanner scanner, HordeSpawnData spawnData) : base(worldSize, scanner, spawnData, 64)
        {
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator()
        {
            return new WorldWildernessWanderingAnimalAICommandGenerator();
        }
    }
}
