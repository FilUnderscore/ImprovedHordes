using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Source.POI
{
    public sealed class WorldPOIScanner
    {
        private readonly List<Zone> zones = new List<Zone>();

        private int highestCount;

        public WorldPOIScanner()
        {
            this.Scan();
        }

        private void Scan()
        {
            DynamicPrefabDecorator dynamicPrefabDecorator = GameManager.Instance.World.ChunkClusters[0].ChunkProvider.GetDynamicPrefabDecorator();
            List<PrefabInstance> pois = dynamicPrefabDecorator.GetPOIPrefabs().ToList();
            
            // Merge POIs into sub-zones.
            for(int i = 0; i < pois.Count - 1; i++) 
            {
                var poi = pois[i];
                var zone = new Zone(poi);

                for(int j = i + 1; j < pois.Count; j++)
                {
                    var other = pois[j];
                    
                    if(zone.GetBounds().Intersects(other.GetAABB()) ||
                        zone.GetBounds().Contains(other.GetAABB().min) ||
                        zone.GetBounds().Contains(other.GetAABB().max))
                    {
                        pois.RemoveAt(j--);
                        zone.Add(other);
                    }
                }

                zone.GetBounds().Expand(zone.GetBounds().size.magnitude);
                zones.Add(zone);
            }

            // Merge nearby sub-zones into larger zones (e.g. cities)

            while (true)
            {
                bool merged = false;

                for (int i = 0; i < zones.Count - 1; i++)
                {
                    var zone = zones[i];

                    for (int j = i + 1; j < zones.Count; j++)
                    {
                        var other = zones[j];

                        if (zone.GetBounds().Intersects(other.GetBounds()) ||
                            zone.GetBounds().Contains(other.GetBounds().min) ||
                            zone.GetBounds().Contains(other.GetBounds().max))
                        {
                            merged |= true;

                            zones.RemoveAt(j--);
                            zone.Merge(other);

                            if (j >= i)
                            {
                                i--;
                                break;
                            }
                        }
                    }
                }

                if (!merged)
                    break;
            }

            highestCount = zones.Max(zone => zone.GetCount());
            Log.Out($"[Improved Hordes] [World POI Scanner] Zones ({zones.Count}) - highest POI count in zones ({highestCount})");

            List<WorldPOIScanner.Zone> toRemove = new List<Zone>();
            foreach(var zone in zones)
            {
                zone.UpdateDensity(this.highestCount);
                Log.Out("Zone density: " + zone.GetDensity() + " Count: " + zone.GetCount());

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

            public Zone(PrefabInstance poi)
            {
                this.bounds = poi.GetAABB();
                this.pois.Add(poi);
            }

            public int GetCount()
            {
                return this.pois.Count;
            }

            public Bounds GetBounds()
            {
                return this.bounds;
            }

            public void Add(PrefabInstance poi)
            {
                this.pois.Add(poi);

                // Recalculate bounds.
                //this.bounds.SetMinMax(Vector3.Min(bounds.min, poi.GetAABB().min), Vector3.Max(bounds.max, poi.GetAABB().max));
                this.bounds.Encapsulate(poi.GetAABB());
            }

            public void Merge(Zone zone)
            {
                this.pois.AddRange(zone.pois);

                // Recalculate bounds
                //this.bounds.SetMinMax(Vector3.Min(bounds.min, zone.bounds.min), Vector3.Max(bounds.max, zone.bounds.max));
                this.bounds.Encapsulate(zone.bounds);
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
