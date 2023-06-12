using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.POI
{
    public sealed class WorldPOIScanner
    {
        private static readonly string[] valid_zones =
        {
            "commercial",
            "commercial,downtown",
            "commercial,downtown,residential",
            "residential,downtown",
            "industrial,downtown",
            "downtown",
            "countrytown",

            "residential,culdesac",
            "residential,countryresidential,culdesac",
        };

        private static readonly string[] invalid_zones =
        {
            "rural"
        };

        private readonly List<Zone> zones = new List<Zone>();

        private int highestCount;

        public WorldPOIScanner()
        {
            this.Scan();
        }

        private static void GetNearby(DynamicPrefabDecorator dynamicPrefabDecorator, PrefabInstance prefab, List<PrefabInstance> nearby, List<PrefabInstance> allowedPois)
        {
            Dictionary<int, PrefabInstance> prefabs = new Dictionary<int, PrefabInstance>();

            dynamicPrefabDecorator.GetPrefabsAround(prefab.boundingBoxPosition + prefab.boundingBoxSize * 0.5f, 128.0f, prefabs);

            foreach (var p in prefabs.Values)
            {
                if (p == prefab || nearby.Contains(p) || !allowedPois.Contains(p) || !IsValid(p))
                    continue;

                nearby.Add(p);
                GetNearby(dynamicPrefabDecorator, p, nearby, allowedPois);
            }
        }

        private static bool IsValid(PrefabInstance poi)
        {
            foreach(var tag in invalid_zones)
            {
                POITags test = POITags.Parse(tag);

                if (poi.prefab.Tags.Test_AnySet(test))
                    return false;
            }

            foreach(var tag in valid_zones)
            {
                POITags test = POITags.Parse(tag);

                if (poi.prefab.Tags.Test_AllSet(test))
                    return true;
            }

            return false;
        }

        private void Scan()
        {
            DynamicPrefabDecorator dynamicPrefabDecorator = GameManager.Instance.World.ChunkClusters[0].ChunkProvider.GetDynamicPrefabDecorator();
            List<PrefabInstance> pois = dynamicPrefabDecorator.GetPOIPrefabs().ToList();
            List<PrefabInstance> allowedPois = dynamicPrefabDecorator.GetPOIPrefabs().ToList();

            // Merge POIs into sub-zones.
            for (int i = 0; i < pois.Count; i++) 
            {
                var poi = pois[i];

                List<PrefabInstance> nearby = new List<PrefabInstance>();
                GetNearby(dynamicPrefabDecorator, poi, nearby, allowedPois);

                foreach(var near in nearby)
                {
                    pois.Remove(near);
                }

                nearby.Add(poi);

                for (int j = 0; j < nearby.Count; j++)
                {
                    if (!IsValid(nearby[j]))
                        nearby.RemoveAt(j--);
                }

                if (nearby.Count > 0)
                    zones.Add(new Zone(nearby));
            }

            // Eliminate overlaps.

            for(int i = 0; i < zones.Count - 1; i++)
            {
                var zone = zones[i];

                for(int j = i + 1; j < zones.Count; j++)
                {
                    var other = zones[j];

                    if(zone.GetBounds().Intersects(other.GetBounds()))
                    {
                        zone.Merge(other);
                        zones.RemoveAt(j--);
                    }
                }
            }

            // Eliminate non-downtown.

            for(int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];

                bool any = false;

                POITags test = POITags.Parse("downtown,countrytown");

                foreach(var p in zone.GetPOIs())
                {
                    if(p.prefab.Tags.Test_AnySet(test))
                    {
                        any = true;
                        break;
                    }
                }

                if (!any)
                {
                    zones.RemoveAt(i--);
                }
            }

            highestCount = zones.Max(zone => zone.GetCount());

            List<WorldPOIScanner.Zone> toRemove = new List<Zone>();
            foreach(var zone in zones)
            {
                zone.UpdateDensity(this.highestCount);
                
                if(zone.GetDensity() <= 0.0f + float.Epsilon)
                {
                    toRemove.Add(zone);
                }
            }

            foreach(var zoneToRemove in toRemove)
            {
                zones.Remove(zoneToRemove);
            }
        }

        public sealed class Zone
        {
            private Bounds bounds;
            private readonly List<PrefabInstance> pois = new List<PrefabInstance>();
            private float density;

            public Zone(List<PrefabInstance> pois)
            {
                this.pois = pois;
                this.RecalculateBounds();
            }

            private void RecalculateBounds()
            {
                Bounds newBounds = pois[0].GetAABB();
                foreach (var poi in this.pois)
                {
                    newBounds.Encapsulate(poi.GetAABB());
                }

                newBounds.Expand(newBounds.size * -0.5f);
                this.bounds = newBounds;
            }

            public List<PrefabInstance> GetPOIs()
            {
                return this.pois;
            }

            public void Merge(Zone other)
            {
                this.pois.AddRange(other.pois);
                this.RecalculateBounds();
            }

            public int GetCount()
            {
                return this.pois.Count;
            }

            public Bounds GetBounds()
            {
                return this.bounds;
            }

            public void UpdateDensity(int highestCount)
            {
                this.density = (this.GetCount() - 1) / (float)(highestCount - 1);
            }

            public float GetDensity()
            {
                return this.density;
            }
        }

        public bool HasScanCompleted()
        {
            return zones.Count > 0;
        }

        public List<WorldPOIScanner.Zone> GetZones()
        {
            return this.zones;
        }
    }
}
