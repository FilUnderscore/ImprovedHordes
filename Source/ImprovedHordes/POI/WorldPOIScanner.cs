using ImprovedHordes.Core.Abstractions.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.POI
{
    public sealed class WorldPOIScanner
    {
        private const float TOWN_WEIGHT = 4.0f;
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
            
            // Get avg weight first of POIS
            for(int i = 0; i < toZone.Count - 1; i++)
            {
                var poi = toZone[i];
                for(int j = i + 1; j < toZone.Count; j++) 
                {
                    var other = toZone[j];

                    float distance = Vector2.Distance(poi.GetLocation(), other.GetLocation());
                    float sizeCombined = (poi.GetBounds().size.magnitude + other.GetBounds().size.magnitude);

                    bool closeEnough = distance <= sizeCombined;

                    if(closeEnough)
                    {
                        poi.MarkZoned();
                        other.MarkZoned();
                    }
                }
            }

            this.logger.Warn("Before weight purge: " + toZone.Count);
            for(int i = 0; i < toZone.Count; i++)
            {
                if (toZone[i].GetWeight() < TOWN_WEIGHT)
                {
                    toZone.RemoveAt(i--);
                }
            }
            this.logger.Warn("After weight purge: " + toZone.Count);

            for(int i = 0; i < toZone.Count - 1; i++)
            {
                POIZone zone = new POIZone(toZone[i]);

                for(int j = i + 1; j < toZone.Count; j++)
                {
                    float distance = Vector2.Distance(toZone[i].GetLocation(), toZone[j].GetLocation());
                    bool nearby = distance <= Mathf.Min(toZone[i].GetBounds().size.magnitude, toZone[j].GetBounds().size.magnitude) / 2f;

                    if(nearby)
                    {
                        float higherWeight = Mathf.Max(toZone[i].GetWeight(), toZone[j].GetWeight());
                        if (toZone[j].GetWeight() >= higherWeight)
                        {
                            zone.Add(toZone[j]);
                            toZone.RemoveAt(j--);
                        }
                        else
                        {
                            this.logger.Info($"Not big enough.");
                        }
                    }
                }

                poiZones.Add(zone);
            }

            // Combine zones

            /*
            // Start zoning.
            for (int i = 0; i < toZone.Count - 1; i++)
            {
                var poi = toZone[i];
                var zone = new POIZone(poi);
                bool valid = false;

                for(int j = i + 1; j < toZone.Count; j++)
                {
                    var other = toZone[j];
                    float distance = Vector2.Distance(poi.GetLocation(), other.GetLocation());
                    float sizeCombined = (poi.GetBounds().size.magnitude + other.GetBounds().size.magnitude) / 2;
                    bool zoned = distance <= sizeCombined;

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
                        //float tolerance = Mathf.Min(zone.GetBounds().size.magnitude, other.GetBounds().size.magnitude) / 2;

                        float tolerance = Mathf.Min(zone.GetAverageDistanceBetweenPOIs(), other.GetAverageDistanceBetweenPOIs());

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

            */
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

            public POIZone(POI poi)
            {
                this.pois.Add(poi);
            }

            public void Add(POI poi)
            {
                this.pois.Add(poi);
            }

            public List<POI> GetPOIs()
            {
                return this.pois;
            }

            public void Merge(params POIZone[] other)
            {
                foreach(var zone in other)
                {
                    zone.pois.ForEach(p => this.Add(p));
                }
            }

            public float GetAverageDistanceBetweenPOIs()
            {
                float avgDist = 0.0f;
                int count = 0;

                for(int i = 0; i < this.pois.Count - 1; i++)
                {
                    for(int j = i + 1; j < this.pois.Count; j++)
                    {
                        avgDist += Vector2.Distance(pois[i].GetLocation(), pois[j].GetLocation());
                        count += 1;
                    }
                }

                return avgDist / count;
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
