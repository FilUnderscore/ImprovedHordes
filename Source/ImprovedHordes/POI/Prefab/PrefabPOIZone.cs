using ImprovedHordes.Core.Abstractions.World.Random;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.POI.Prefab
{
    public sealed class PrefabPOIZone
    {
        internal static int HIGHEST_COUNT;

        private List<PrefabPOI> pois = new List<PrefabPOI>();
        internal BiomeDefinition biome;

        public PrefabPOIZone(PrefabPOI poi)
        {
            this.pois.Add(poi);
        }

        public void Add(PrefabPOI poi)
        {
            this.pois.Add(poi);
        }

        public List<PrefabPOI> GetPOIs()
        {
            return this.pois;
        }

        public void Merge(PrefabPOIZone other)
        {
            other.pois.ForEach(z => this.pois.Add(z));
        }

        public int GetCount()
        {
            return this.pois.Count;
        }

        public Vector2 GetCenter()
        {
            Vector2 center = this.pois[0].GetLocation();

            for (int i = 1; i < this.pois.Count; i++)
            {
                center += this.pois[i].GetLocation();
            }

            center /= this.pois.Count;
            return center;
        }

        public Bounds GetBounds()
        {
            Bounds bounds = this.pois[0].GetBounds();

            for (int i = 1; i < this.pois.Count; i++)
            {
                bounds.Encapsulate(this.pois[i].GetBounds());
            }

            return bounds;
        }

        public float GetDensity()
        {
            return (float)this.GetCount() / HIGHEST_COUNT;
        }

        public void GetLocationOutside(IWorldRandom worldRandom, out Vector2 location)
        {
            if (GameManager.Instance.World.ChunkClusters?[0]?.ChunkProvider?.GetDynamicPrefabDecorator() != null) // NRE fix for LCB/trader area detection.
            {
                List<PrefabPOI> remainingPOIs = this.pois.ToList();
                PrefabPOI randomPOI;

                do
                {
                    randomPOI = worldRandom.Random<PrefabPOI>(this.pois);

                    if (!randomPOI.IsPlayerConvertedPOI())
                    {
                        randomPOI.GetLocationOutside(worldRandom, out location);
                        remainingPOIs.Clear();

                        return;
                    }

                    remainingPOIs.Remove(randomPOI);
                } while (remainingPOIs.Count > 0);
            }

            // If all zone POIs have land claim blocks nearby, then spawn on the outskirts of the zone.
            float size = this.GetBounds().size.magnitude / 2;
            location = this.GetCenter() + worldRandom.RandomOnUnitCircle * size;
        }

        public BiomeDefinition GetBiome()
        {
            return this.biome;
        }
    }
}