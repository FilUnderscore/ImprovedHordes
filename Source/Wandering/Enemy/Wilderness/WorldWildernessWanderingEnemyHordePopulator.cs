using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.POI;

namespace ImprovedHordes.Wandering.Enemy.Wilderness
{
    public sealed class WorldWildernessWanderingEnemyHordePopulator : WorldWildernessHordePopulator<WanderingEnemyHorde>
    {
        public WorldWildernessWanderingEnemyHordePopulator(float worldSize, WorldPOIScanner scanner, HordeSpawnData spawnData) : base(worldSize, scanner, spawnData)
        {
        }

        public override IAICommandGenerator CreateHordeAICommandGenerator()
        {
            return new GoToWorldZoneAICommandGenerator(this.scanner);
        }
    }
}
