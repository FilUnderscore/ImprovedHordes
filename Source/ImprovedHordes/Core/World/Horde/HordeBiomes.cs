using ImprovedHordes.Core.Abstractions.Settings;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde
{
    public static class HordeBiomes
    {
        private static readonly Setting<float> HORDE_BIOME_MULTIPLIER = new Setting<float>("horde_biome_multiplier", 0.5f);
        private static readonly Setting<float> HORDE_BIOME_CURVE_SCALE = new Setting<float>("horde_biome_curve_scale", 1.5f);

        public static BiomeDefinition GetBiomeAt(Vector2 location)
        {
            return GameManager.Instance.World?.ChunkCache?.ChunkProvider?.GetBiomeProvider()?.GetBiomeAt((int)location.x, (int)location.y);
        }

        public static float DetermineMaxBiomeDensity(Vector3 location)
        {
            return DetermineMaxBiomeDensity(new Vector2(location.x, location.z));
        }

        public static float DetermineMaxBiomeDensity(Vector2 location)
        {
            BiomeDefinition biome = GetBiomeAt(location);

            if (biome == null)
                return 1.0f;

            float maxHordeDensity = WorldHordeTracker.MAX_HORDE_DENSITY.Value - 1.0f;
            return 1.0f + (maxHordeDensity - (maxHordeDensity / biome.Difficulty));
        }

        public static float DetermineBiomeSparsityFactor(Vector3 location)
        {
            return DetermineBiomeSparsityFactor(new Vector2(location.x, location.z));
        }

        public static float DetermineBiomeSparsityFactor(Vector2 location)
        {
            BiomeDefinition biome = GetBiomeAt(location);
            float biomeMultiplier = HORDE_BIOME_MULTIPLIER.Value;

            if (biome == null)
                return biomeMultiplier;

            return biomeMultiplier + Mathf.Pow(HORDE_BIOME_CURVE_SCALE.Value, biome.Difficulty - 4.0f);
        }
    }
}
