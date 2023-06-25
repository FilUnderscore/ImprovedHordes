using ImprovedHordes.Core.Abstractions.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.POI
{
    public sealed class WorldPOIScanner
    {
        private const float POI_MERGE_DIST = 84.0f;
        private readonly Core.Abstractions.Logging.ILogger logger;

        private static int HIGHEST_COUNT;

        private readonly List<POI> pois = new List<POI>();
        private readonly List<POIZone> zones = new List<POIZone>();

        public WorldPOIScanner(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.Create(typeof(WorldPOIScanner));
            this.ScanZones();
        }

        public bool HasScanCompleted()
        {
            return this.zones.Count > 0;
        }

        private void ScanZones()
        {
            DynamicPrefabDecorator dynamicPrefabDecorator = GameManager.Instance.World.ChunkClusters[0].ChunkProvider.GetDynamicPrefabDecorator();
            List<PrefabInstance> prefabs = dynamicPrefabDecorator.GetPOIPrefabs();

            foreach(var prefab in prefabs)
            {
                this.pois.Add(new POI(prefab));
            }

            List<POI> toZone = new List<POI>(this.pois);
            List<POIZone> poiZones = new List<POIZone>();

            for(int i = 0; i < toZone.Count - 1; i++)
            {
                var poi = toZone[i];
                var zone = new POIZone(poi);
                bool valid = false;

                for(int j = i + 1; j < toZone.Count; j++)
                {
                    var other = toZone[j];
                    float distance = Vector2.Distance(poi.GetLocation(), other.GetLocation());
                    bool zoned = distance <= POI_MERGE_DIST;

                    if (zoned)
                    {
                        zone.Add(other);
                        toZone.RemoveAt(j--);
                    }
                }

                valid = zone.GetCount() > 1;

                if (valid)
                    poiZones.Add(zone);
            }

            // Iterate and merge zones. average

            const int ITERATIONS = 20;
            int iteration = 0;

            while(iteration++ < ITERATIONS)
            { 
                for(int i = 0; i < poiZones.Count - 1; i++)
                {
                    var zone = poiZones[i];
                    var nearby = new List<POIZone>();

                    for(int j = i + 1; j < poiZones.Count; j++)
                    {
                        var other = poiZones[j];
                        float distance = Vector2.Distance(zone.GetCenter(), other.GetCenter());

                        //float tolerance = (zone.GetAverageDistanceBetweenZones() + other.GetAverageDistanceBetweenZones()) / 2.0f;
                        float tolerance = Mathf.Min(zone.GetAverageDistanceBetweenZones(), other.GetAverageDistanceBetweenZones());

                        if (distance <= tolerance)
                        {
                            nearby.Add(other);
                            poiZones.RemoveAt(j--);
                        }
                    }

                    zone.Merge(nearby.ToArray());
                    this.logger.Verbose($"ITERATION {iteration} : merged into {i} - new count " + zone.GetCount());
                }
            }

            float avgWeight = poiZones.Average(zone => zone.GetAverageWeight());
            for(int z = 0; z < poiZones.Count; z++)
            {
                var zone = poiZones[z];
                //float avgWeight = zone.GetAverageWeight();

                for (int i = 0; i < zone.GetPOIs().Count; i++)
                {
                    if (zone.GetPOIs()[i].GetWeight() < avgWeight)
                    {
                        zone.GetPOIs().RemoveAt(i--);
                    }
                }

                if (zone.GetCount() <= 1)
                {
                    poiZones.RemoveAt(z--);
                }
            }

            zones.AddRange(poiZones);
            HIGHEST_COUNT = zones.Max(zone => zone.GetCount());
        }

        public List<POIZone> GetZones()
        {
            return this.zones;
        }

        public sealed class POIZone
        {
            private List<POI> pois = new List<POI>();
            private float averageDistanceBetweenZones = POI_MERGE_DIST;

            public POIZone(POI poi)
            {
                this.pois.Add(poi);
            }

            public void Add(POI poi)
            {
                this.pois.ForEach(p => p.MarkZoned());
                this.pois.Add(poi);

                poi.MarkZoned();
            }

            public List<POI> GetPOIs()
            {
                return this.pois;
            }

            public void Merge(params POIZone[] other)
            {
                averageDistanceBetweenZones = 0.0f;

                foreach(var zone in other)
                {
                    float distance = Vector2.Distance(zone.GetCenter(), this.GetCenter());
                    averageDistanceBetweenZones += distance;

                    zone.pois.ForEach(p => this.Add(p));
                }

                averageDistanceBetweenZones /= other.Length;
            }

            public float GetAverageDistanceBetweenZones()
            {
                return this.averageDistanceBetweenZones;
            }

            public float GetAverageWeight()
            {
                return this.pois.Average(poi => poi.GetWeight());
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

                for(int i = 1; i < this.pois.Count; i++)
                {
                    bounds.Encapsulate(this.pois[i].GetBounds());
                }

                return bounds;
            }

            public float GetDensity()
            {
                return ((float)this.GetCount() / HIGHEST_COUNT) * this.GetAverageWeight();
            }
        }

        public sealed class POI
        {
            private PrefabInstance prefab;
            private float weight;

            public POI(PrefabInstance prefab)
            {
                this.prefab = prefab;
                this.weight = 0.0f;
            }

            public void MarkZoned()
            {
                this.weight += 1.0f;
            }

            public float GetWeight()
            {
                return this.weight;
            }

            public Vector2 GetLocation()
            {
                return this.prefab.GetCenterXZ();
            }

            public Bounds GetBounds()
            {
                return this.prefab.GetAABB();
            }
        }
    }
}
