using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.World.Horde;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ImprovedHordes.POI.Prefab;

namespace ImprovedHordes.POI
{
    public sealed class WorldPrefabPOIScanner
    {
        private float avgZoneDensity;

        private const float TOWN_WEIGHT = 3.0f;
        private readonly Core.Abstractions.Logging.ILogger logger;

        private readonly List<PrefabPOI> pois = new List<PrefabPOI>();

        private readonly List<PrefabPOIZone> zones = new List<PrefabPOIZone>();
        
        private readonly WorldPOITracker tracker;

        public WorldPrefabPOIScanner(ILoggerFactory loggerFactory, WorldPOITracker tracker)
        {
            this.logger = loggerFactory.Create(typeof(WorldPrefabPOIScanner));
            this.tracker = tracker;

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

            foreach (var prefab in prefabs)
            {
                this.pois.Add(new PrefabPOI(prefab));
            }

            List<PrefabPOI> toZone = new List<PrefabPOI>(this.pois);
            List<PrefabPOIZone> poiZones = new List<PrefabPOIZone>();

            // Get avg weight first of POIS
            for (int i = 0; i < toZone.Count - 1; i++)
            {
                var poi = toZone[i];
                for (int j = i + 1; j < toZone.Count; j++)
                {
                    var other = toZone[j];

                    float distance = Vector2.Distance(poi.GetLocation(), other.GetLocation());
                    float sizeCombined = (poi.GetBounds().size.magnitude + other.GetBounds().size.magnitude);

                    bool closeEnough = distance <= sizeCombined;

                    if (closeEnough)
                    {
                        poi.MarkZoned(other);
                        other.MarkZoned(poi);
                    }
                }
            }

            float townWeight = toZone.Count > 0 ? Mathf.Min(TOWN_WEIGHT, toZone.Average(poi => poi.GetWeight())) : TOWN_WEIGHT;
            //this.logger.Info("Town Weight: " + townWeight);
            //this.logger.Info("Avg: " + toZone.Average(poi => poi.GetWeight()));

            for (int i = 0; i < toZone.Count; i++)
            {
                if (toZone[i].GetWeight() < townWeight)
                {
                    toZone.RemoveAt(i--);
                }
            }

            for (int i = 0; i < toZone.Count - 1; i++)
            {
                PrefabPOIZone zone = new PrefabPOIZone(toZone[i]);

                for (int j = i + 1; j < toZone.Count; j++)
                {
                    float distance = Vector2.Distance(toZone[i].GetLocation(), toZone[j].GetLocation());
                    bool nearby = distance <= Mathf.Min(toZone[i].GetBounds().size.magnitude, toZone[j].GetBounds().size.magnitude) / 2f;

                    if (nearby)
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
                    var near = new List<PrefabPOIZone>();

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

                for (int i = 0; i < poiZones.Count - 1; i++)
                {
                    var zone = poiZones[i];
                    var zonePOI = zone.GetPOIs();

                    for (int j = i + 1; j < poiZones.Count; j++)
                    {
                        bool done = false;

                        var other = poiZones[j];
                        var otherPOI = other.GetPOIs();

                        for (int zp = 0; zp < zonePOI.Count; zp++)
                        {
                            var zPOI = zonePOI[zp];

                            for (int op = 0; op < otherPOI.Count; op++)
                            {
                                var oPOI = otherPOI[op];

                                float distance = Vector2.Distance(zPOI.GetLocation(), oPOI.GetLocation());
                                bool nearby = distance <= (zPOI.GetBounds().size.magnitude + oPOI.GetBounds().size.magnitude);

                                if (nearby)
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

            foreach(var zone in poiZones)
            {
                BiomeDefinition biome = HordeBiomes.GetBiomeAt(zone.GetCenter(), true);

                if(!biomeZones.TryGetValue(biome, out var biomeZonesList))                
                {
                    biomeZones.Add(biome, biomeZonesList = new List<PrefabPOIZone>());
                }

                biomeZones[biome].Add(zone);
                zone.biome = biome;
            }

            zones.AddRange(poiZones);

            if (!zones.Any())
            {
                this.logger.Warn("Failed to detect POI zones in the world. This should only happen if there are no cities generated.");
                return;
            }

            // Calculate zone density.
            PrefabPOIZone.HIGHEST_COUNT = zones.Max(z => z.GetCount());
            avgZoneDensity = zones.Average(z => z.GetDensity());
        }

        public float GetAverageZoneDensity()
        {
            return this.avgZoneDensity;
        }

        public List<PrefabPOIZone> GetAllZones()
        {
            return this.zones;
        }

        public List<PrefabPOIZone> GetBiomeZones(BiomeDefinition biome)
        {
            if(biome != null && this.biomeZones.TryGetValue(biome, out var biomeZoneList))
                return biomeZoneList;

            return this.GetAllZones();
        }

        public PrefabPOI GetPOIAt(Vector3 location)
        {
            foreach (var zone in this.zones)
            {
                foreach (var poi in zone.GetPOIs())
                {
                    if (poi.GetBounds().Contains(location))
                        return poi;
                }
            }

            return null;
        }
    }
}
