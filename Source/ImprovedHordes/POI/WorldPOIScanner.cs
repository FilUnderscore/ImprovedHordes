using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.World.Random;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.POI
{
    public sealed class WorldPOIScanner
    {
        private static int HIGHEST_COUNT;
        private float avgZoneDensity;

        private const float TOWN_WEIGHT = 3.0f;
        private readonly Core.Abstractions.Logging.ILogger logger;

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
                        poi.MarkZoned(other);
                        other.MarkZoned(poi);
                    }
                }
            }

            for(int i = 0; i < toZone.Count; i++)
            {
                if (toZone[i].GetWeight() < TOWN_WEIGHT)
                {
                    toZone.RemoveAt(i--);
                }
            }

            for(int i = 0; i < toZone.Count - 1; i++)
            {
                POIZone zone = new POIZone(toZone[i]);

                for(int j = i + 1; j < toZone.Count; j++)
                {
                    float distance = Vector2.Distance(toZone[i].GetLocation(), toZone[j].GetLocation());
                    bool nearby = distance <= Mathf.Min(toZone[i].GetBounds().size.magnitude, toZone[j].GetBounds().size.magnitude) / 2f;

                    if(nearby)
                    {
                        float higherWeight = (toZone[i].GetWeight() + toZone[j].GetWeight()) / 2.0f;
                        if (toZone[j].GetWeight() >= higherWeight)
                        {
                            zone.Add(toZone[j]);
                            toZone.RemoveAt(j--);
                        }
                    }
                }

                poiZones.Add(zone);
            }

            // Merge
            bool merge;

            do
            {
                merge = false;

                for (int i = 0; i < poiZones.Count - 1; i++)
                {
                    var zone = poiZones[i];
                    var near = new List<POIZone>();

                    for (int j = i + 1; j < poiZones.Count; j++)
                    {
                        var other = poiZones[j];

                        float distance = Vector2.Distance(zone.GetCenter(), other.GetCenter());
                        bool nearby = distance <= Mathf.Min(zone.GetBounds().size.magnitude, other.GetBounds().size.magnitude) / 2f;

                        if (nearby)
                        {
                            near.Add(other);
                            poiZones.RemoveAt(j--);
                        }
                    }

                    foreach (var n in near)
                    {
                        zone.Merge(n);
                    }

                    merge |= near.Any();
                }
            } while (merge);

            // Combine smaller zones into bigger by taking POIs in zones and checking distance is near.

            do
            {
                merge = false;

                for(int i = 0; i < poiZones.Count - 1; i++)
                {
                    var zone = poiZones[i];
                    var zonePOI = zone.GetPOIs();
                    
                    for(int j = i + 1; j < poiZones.Count; j++)
                    {
                        bool done = false;

                        var other = poiZones[j];
                        var otherPOI = other.GetPOIs();

                        for(int zp = 0; zp < zonePOI.Count; zp++)
                        {
                            var zPOI = zonePOI[zp];

                            for(int op = 0; op < otherPOI.Count; op++)
                            {
                                var oPOI = otherPOI[op];

                                float distance = Vector2.Distance(zPOI.GetLocation(), oPOI.GetLocation());
                                bool nearby = distance <= (zPOI.GetBounds().size.magnitude + oPOI.GetBounds().size.magnitude);

                                if(nearby)
                                {
                                    zone.Merge(other);
                                    poiZones.RemoveAt(j--);
                                    done = true;
                                    merge |= true;
                                    break;
                                }
                            }

                            if (done)
                                break;
                        }

                        done = false;
                    }
                }
            } while (merge);

            zones.AddRange(poiZones);

            // Calculate zone density.
            HIGHEST_COUNT = poiZones.Max(z => z.GetCount());
            avgZoneDensity = zones.Average(z => z.GetDensity());
        }

        public float GetAverageZoneDensity()
        {
            return this.avgZoneDensity;
        }

        public List<POIZone> GetZones()
        {
            return this.zones;
        }

        public POI GetPOIAt(Vector3 location)
        {
            foreach(var zone in this.zones) 
            {
                foreach(var poi in zone.GetPOIs())
                {
                    if (poi.GetBounds().Contains(location))
                        return poi;
                }
            }

            return null;
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

            public void Merge(POIZone other)
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

                for(int i = 1; i < this.pois.Count; i++)
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
                List<POI> remainingPOIs = this.pois.ToList();
                POI randomPOI;

                do
                {
                    randomPOI = worldRandom.Random<POI>(this.pois);

                    if (!randomPOI.IsPlayerConvertedPOI())
                    {
                        randomPOI.GetLocationOutside(worldRandom, out location);
                        remainingPOIs.Clear();

                        return;
                    }

                    remainingPOIs.Remove(randomPOI);
                } while (remainingPOIs.Count > 0);

                // If all zone POIs have land claim blocks nearby, then spawn on the outskirts of the zone.
                float size = this.GetBounds().size.magnitude / 2;
                location = this.GetCenter() + worldRandom.RandomOnUnitCircle * size;
            }
        }

        public sealed class POI
        {
            private PrefabInstance prefab;
            private float weight;

            private POI closestPOI;

            public POI(PrefabInstance prefab)
            {
                this.prefab = prefab;
                this.weight = 0.0f;
            }

            public void MarkZoned(POI other)
            {
                this.weight += 1.0f;

                if (this.closestPOI == null || Vector2.Distance(this.GetLocation(), other.GetLocation()) < Vector2.Distance(closestPOI.GetLocation(), other.GetLocation()))
                    this.closestPOI = other;
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

            public void GetLocationOutside(IWorldRandom worldRandom, out Vector2 location)
            {
                float minRange = this.GetBounds().size.magnitude / 2;

                if(this.closestPOI == null)
                {
                    location = this.GetLocation() + worldRandom.RandomOnUnitCircle * minRange;
                    return;
                }

                float closestPOIDistance = Vector2.Distance(this.GetLocation(), this.closestPOI.GetLocation());
                float closestPOIMinRange = this.closestPOI.GetBounds().size.magnitude / 2;

                float maxRange = closestPOIDistance - closestPOIMinRange;

                float range = worldRandom.RandomFloat * (maxRange - minRange) + minRange;
                location = this.GetLocation() + worldRandom.RandomOnUnitCircle * range;
            }

            public bool IsPlayerConvertedPOI()
            {
                return GameManager.Instance.World.GetLandClaimOwner(new Vector3i(this.GetBounds().center), null) != EnumLandClaimOwner.None;
            }
        }
    }
}
