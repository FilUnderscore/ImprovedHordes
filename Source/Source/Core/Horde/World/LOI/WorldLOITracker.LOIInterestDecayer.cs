﻿using ImprovedHordes.Source.Horde.World.LOI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Thread = ImprovedHordes.Source.Utils.Thread;

namespace ImprovedHordes.Source.Core.Horde.World.LOI
{
    public sealed partial class WorldLOITracker
    {
        private sealed class LOIInterestDecayer : Thread
        {
            private const double LOG_N_100 = 4.60517018599;
            private readonly double MAP_SIZE_LOG_N;
            private readonly double MAP_SIZE_POW_2_LOG_N;

            private WorldLOITracker tracker;

            // Shared            
            private readonly List<LocationOfInterest> reportedLocations = new List<LocationOfInterest>();
            private readonly object reportedLocationsLock = new object();

            // Personal
            private readonly Dictionary<Vector2i, LocationOfInterest> locationHistory = new Dictionary<Vector2i, LocationOfInterest>();
            private readonly List<Vector2i> locationsToRemove = new List<Vector2i>();
            private readonly List<LOIInterestNotificationEvent> eventsToReport = new List<LOIInterestNotificationEvent>();

            public LOIInterestDecayer(WorldLOITracker tracker, float mapSize) : base("IH-LOIInterestDecayer")
            {
                this.tracker = tracker;

                this.MAP_SIZE_LOG_N = Math.Log(mapSize);
                this.MAP_SIZE_POW_2_LOG_N = Math.Pow(2, MAP_SIZE_LOG_N);
            }

            public void Notify(ConcurrentBag<LocationOfInterest> locations)
            {
                if (locations != null)
                {
                    Monitor.Enter(this.reportedLocationsLock);
                    
                    while(locations.TryTake(out LocationOfInterest location))
                        this.reportedLocations.Add(location);

                    Monitor.Exit(this.reportedLocationsLock);
                }
            }

            public override bool OnLoop()
            {
                // First register/modify existing locations.
                if (Monitor.TryEnter(this.reportedLocationsLock))
                {
                    if (this.reportedLocations.Count > 0)
                    {
                        foreach (var locationOfInterest in this.reportedLocations)
                        {
                            Vector2i location = locationOfInterest.GetChunkLocation();
                            float interestLevel;
                            
                            if (locationHistory.TryGetValue(location, out LocationOfInterest firstLocationReport))
                            {
                                firstLocationReport.Add(locationOfInterest);
                                interestLevel = firstLocationReport.GetInterestLevel();
                            }
                            else
                            {
                                locationHistory.Add(location, locationOfInterest);
                                interestLevel = locationOfInterest.GetInterestLevel();
                            }

                            eventsToReport.Add(new LOIInterestNotificationEvent(locationOfInterest.GetLocation(), interestLevel, CalculateInterestDistance(interestLevel)));
                        }

                        this.reportedLocations.Clear();
                    }

                    Monitor.Exit(this.reportedLocationsLock);
                }

                // Report events
                foreach (var entry in this.locationHistory)
                {
                    LocationOfInterest locationOfInterest = entry.Value;
                    float interestLevel = locationOfInterest.GetInterestLevel();

                    if (interestLevel <= 0.0)
                    {
                        locationsToRemove.Add(entry.Key);
                    }
                }

                // Notify game thread of events, wait for lock if needed.
                if (eventsToReport.Count > 0)
                {
                    Monitor.Enter(this.tracker.EventsLock);
                    this.tracker.Events.AddRange(this.eventsToReport);
                    Monitor.Exit(this.tracker.EventsLock);

                    if(this.tracker.OnInterestNotificationEventThread != null)
                    {
                        foreach(LOIInterestNotificationEvent notificationEvent in this.eventsToReport)
                        {
                            this.tracker.OnInterestNotificationEventThread.Invoke(this, notificationEvent);
                        }
                    }

                    this.eventsToReport.Clear();
                }

                foreach (Vector2i location in locationsToRemove)
                {
                    this.locationHistory.Remove(location);
                }

                locationsToRemove.Clear();

                return true;
            }

            /// <summary>
            /// The distance at which hordes take interest.
            /// </summary>
            /// <param name="mapSize"></param>
            /// <param name="interestLevel"></param>
            /// <returns></returns>
            private int CalculateInterestDistance(float interestLevel)
            {
                double mapScaleFactor = MAP_SIZE_POW_2_LOG_N;
                double mapOffsetFactor = MAP_SIZE_LOG_N + LOG_N_100;

                double distance = mapScaleFactor * (interestLevel / 100.0) + mapOffsetFactor;

                return (int)distance;
            }

        }
    }
}
