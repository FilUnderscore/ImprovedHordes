using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.POI;

namespace ImprovedHordes.Source.Wandering
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
