using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Source.POI
{
    public sealed class WorldPOIScanner
    {
        private const float CITY_DENSITY_THRESHOLD = 0.5f;

        private readonly List<Zone> zones = new List<Zone>();

        private float highestDensity;

        public WorldPOIScanner()
        {
            this.Scan();
        }

        private void Scan()
        {
            DynamicPrefabDecorator dynamicPrefabDecorator = GameManager.Instance.World.ChunkClusters[0].ChunkProvider.GetDynamicPrefabDecorator();
            List<PrefabInstance> pois = dynamicPrefabDecorator.GetPOIPrefabs().ToList();
            
            // Merge POIs into sub-zones.
            for(int i = 0; i < pois.Count; i++) 
            {
                var poi = pois[i];
                var zone = new Zone(poi);

                for(int j = i + 1; j < pois.Count; j++)
                {
                    var other = pois[j];
                    float sqrDistance = zone.GetBounds().SqrDistance(other.GetAABB().center);
                    float size = zone.GetBounds().size.sqrMagnitude * (other.GetAABB().size.sqrMagnitude / zone.GetBounds().size.sqrMagnitude);

                    if(zone.GetBounds().Intersects(other.GetAABB()) || sqrDistance <= size)
                    {
                        pois.RemoveAt(j--);
                        zone.Add(other);
                    }
                }

                zones.Add(zone);
            }

            // Merge nearby sub-zones into larger zones (e.g. cities)
            for(int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];

                for(int j = i + 1; j < zones.Count; j++)
                {
                    var other = zones[j];
                    /*
                    float sqrDistance = zone.GetBounds().SqrDistance(other.GetBounds().center);
                    float size = zone.GetBounds().size.sqrMagnitude * (other.GetBounds().size.sqrMagnitude / zone.GetBounds().size.sqrMagnitude);
                    */

                    if (zone.GetBounds().Intersects(other.GetBounds())/* || sqrDistance <= size*/)
                    {
                        zones.RemoveAt(j--);
                        zone.Merge(other);
                    }
                }
            }

            highestDensity = zones.Max(zone => zone.GetCount());
            Log.Out($"[Improved Hordes] [World POI Scanner] Zones ({zones.Count}) - highest POI count in zones ({highestDensity})");
        }

        public sealed class Zone
        {
            private Bounds bounds;
            private readonly List<PrefabInstance> pois = new List<PrefabInstance>();

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
                this.bounds.SetMinMax(Vector3.Min(bounds.min, poi.GetAABB().min), Vector3.Max(bounds.max, poi.GetAABB().max));
            }

            public void Merge(Zone zone)
            {
                this.pois.AddRange(zone.pois);

                // Recalculate bounds
                this.bounds.SetMinMax(Vector3.Min(bounds.min, zone.bounds.min), Vector3.Max(bounds.max, zone.bounds.max));
            }
        }

        public float GetDensity(Zone zone)
        {
            return zone.GetCount() / this.highestDensity;
        }

        public Zone PickRandomZone()
        {
            return zones.RandomObject();
        }

        public bool HasScanCompleted()
        {
            return zones.Count > 0;
        }

        public int GetZoneCount()
        {
            return zones.Count;
        }
    }
}
