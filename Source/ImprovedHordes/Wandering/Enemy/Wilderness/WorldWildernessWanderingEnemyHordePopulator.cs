using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.POI;

namespace ImprovedHordes.Wandering.Enemy.Wilderness
{
    public sealed class WorldWildernessWanderingEnemyHordePopulator : WorldWildernessHordePopulator<WanderingEnemyHorde>
    {
        public WorldWildernessWanderingEnemyHordePopulator(WorldHordeTracker tracker, float worldSize, WorldPOIScanner scanner, HordeSpawnData spawnData) : base(tracker, worldSize, scanner, spawnData)
        {
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator()
        {
            return new GoToWorldZoneAICommandGenerator(this.scanner);
        }
    }
}
