using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.POI;

namespace ImprovedHordes.Wandering.Enemy.Wilderness
{
    public sealed class WorldWildernessWanderingEnemyHordePopulator : WorldWildernessHordePopulator<WanderingEnemyHorde>
    {
        private static readonly Setting<int> WANDERING_ENEMY_WILDERNESS_SPARSITY = new Setting<int>("wandering_enemy_wilderness_sparsity", 32);

        public WorldWildernessWanderingEnemyHordePopulator(float worldSize, WorldPOIScanner scanner, HordeSpawnParams spawnData) : base(worldSize, scanner, spawnData, WANDERING_ENEMY_WILDERNESS_SPARSITY.Value, true)
        {
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(BiomeDefinition biome)
        {
            return new WorldWildernessWanderingEnemyAICommandGenerator(this.scanner, biome);
        }
    }
}
