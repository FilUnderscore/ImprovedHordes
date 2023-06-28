using ImprovedHordes.Core.Abstractions.Settings;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde
{
    public static class HordeBiomes
    {
        private static readonly Setting<float> HORDE_BIOME_MULTIPLIER = new Setting<float>("horde_biome_multiplier", 0.1f);
        
        public static BiomeDefinition GetBiomeAt(Vector2 location)
        {
            return GameManager.Instance.World.ChunkCache.ChunkProvider.GetBiomeProvider().GetBiomeAt((int)location.x, (int)location.y);
        }

        public static float DetermineBiomeDensity(Vector3 location)
        {
            return DetermineBiomeDensity(new Vector2(location.x, location.z));
        }

        public static float DetermineBiomeDensity(Vector2 location)
        {
            BiomeDefinition biome = GetBiomeAt(location);

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
            BiomeDefinition biome = GetBiomeAt(location);
            float biomeMultiplier = HORDE_BIOME_MULTIPLIER.Value;

            if (biome == null)
                return biomeMultiplier;

            return biomeMultiplier + Mathf.Pow(5.0f, biome.Difficulty - 4.0f);
        }
    }
}
