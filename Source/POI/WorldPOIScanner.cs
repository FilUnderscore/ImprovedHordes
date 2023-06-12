using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.POI
{
    public sealed class WorldPOIScanner
    {
        private readonly List<Zone> zones = new List<Zone>();

        private int highestCount;

        public WorldPOIScanner()
        {
            this.Scan();
        }

        private static void GetNearby(DynamicPrefabDecorator dynamicPrefabDecorator, PrefabInstance prefab, List<PrefabInstance> nearby, List<PrefabInstance> allowedPois)
        {
            Dictionary<int, PrefabInstance> prefabs = new Dictionary<int, PrefabInstance>();
            dynamicPrefabDecorator.GetPrefabsAround(prefab.boundingBoxPosition + prefab.boundingBoxSize * 0.5f, 32.0f, prefabs);

            foreach(var p in prefabs.Values)
            {
                if (p == prefab || nearby.Contains(p) || !allowedPois.Contains(p))
                    continue;

                nearby.Add(p);
                GetNearby(dynamicPrefabDecorator, p, nearby, allowedPois);
            }
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
                zones.Add(new Zone(nearby));
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

                Bounds newBounds = pois[0].GetAABB();
                foreach (var poi in this.pois)
                {
                    newBounds.Encapsulate(poi.GetAABB());
                }

                this.bounds = newBounds;
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
