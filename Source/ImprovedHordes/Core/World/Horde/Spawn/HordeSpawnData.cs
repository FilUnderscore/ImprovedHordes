namespace ImprovedHordes.Core.World.Horde.Spawn
{
    public readonly struct HordeSpawnData
    {
        public readonly HordeSpawnParams SpawnParams;
        public readonly BiomeDefinition SpawnBiome;

        public HordeSpawnData(HordeSpawnParams spawnParams, BiomeDefinition biome)
        {
            this.SpawnParams = spawnParams;
            this.SpawnBiome = biome;
        }
    }
}
