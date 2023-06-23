using ImprovedHordes.Core.Abstractions.Settings;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde
{
    public static class HordeBiomes
    {
        private static readonly Setting<float> HORDE_BIOME_SPARSITY_MULTIPLIER = new Setting<float>("horde_biome_sparsity_multiplier", 0.5f);

        public static float DetermineBiomeDensity(Vector3 location)
        {
            return DetermineBiomeDensity(new Vector2(location.x, location.z));
        }

        public static float DetermineBiomeDensity(Vector2 location)
        {
            BiomeDefinition biome = GameManager.Instance.World.GetBiome((int)location.x, (int)location.y);

            if (biome == null)
                return 1.0f;

            float maxHordeDensity = WorldHordeTracker.MAX_HORDE_DENSITY.Value - 1.0f;
            return 1.0f + (maxHordeDensity - (maxHordeDensity / biome.Difficulty));
        }

        public static float DetermineBiomeFactor(Vector3 location)
        {
            return DetermineBiomeFactor(new Vector2(location.x, location.z));
        }

        public static float DetermineBiomeFactor(Vector2 location)
        {
            BiomeDefinition biome = GameManager.Instance.World.GetBiome((int)location.x, (int)location.y);
            float biomeSparsityMultiplier = HORDE_BIOME_SPARSITY_MULTIPLIER.Value;

            if (biome == null)
                return biomeSparsityMultiplier;

            return biomeSparsityMultiplier / biome.Difficulty;
        }
    }
}
