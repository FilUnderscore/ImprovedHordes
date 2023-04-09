using ImprovedHordes.Source.Horde.World.LOI;
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
            private readonly ConcurrentQueue<LocationOfInterest> reportedLocations = new ConcurrentQueue<LocationOfInterest>();
            private readonly AutoResetEvent waitEvent = new AutoResetEvent(false);

            // Personal
            private readonly Dictionary<Vector2i, LocationOfInterest> locations = new Dictionary<Vector2i, LocationOfInterest>();
            private readonly List<Vector2i> locationsToRemove = new List<Vector2i>();
            private readonly List<LOIInterestNotificationEvent> eventsToReport = new List<LOIInterestNotificationEvent>();

            public LOIInterestDecayer(WorldLOITracker tracker, float mapSize) : base("IH-LOIInterestDecayer")
            {
                this.tracker = tracker;

                this.MAP_SIZE_LOG_N = Math.Log(mapSize);
                this.MAP_SIZE_POW_2_LOG_N = Math.Pow(2, MAP_SIZE_LOG_N);
            }

            public void Notify(List<LocationOfInterest> locations)
            {
                if (locations != null)
                {
                    foreach (LocationOfInterest location in locations)
                    {
                        this.reportedLocations.Enqueue(location);
                    }
                }

                this.waitEvent.Set();
            }

            public override bool OnLoop()
            {
                this.waitEvent.WaitOne();

                // First register/modify existing locations.
                while (this.reportedLocations.TryDequeue(out LocationOfInterest locationOfInterest))
                {
                    Vector2i location = locationOfInterest.GetChunkLocation();

                    if (locations.TryGetValue(location, out LocationOfInterest firstLocationReport))
                    {
                        firstLocationReport.Add(locationOfInterest.GetInterestLevel());
                    }
                    else
                    {
                        locations.Add(location, locationOfInterest);
                    }
                }

                // Report events
                foreach (var entry in this.locations)
                {
                    LocationOfInterest locationOfInterest = entry.Value;
                    float interestLevel = locationOfInterest.GetInterestLevel();

                    if (interestLevel > 0.0)
                    {
                        int distance = CalculateInterestDistance(locationOfInterest.GetInterestLevel());

                        if (distance > 0)
                        {
                            eventsToReport.Add(new LOIInterestNotificationEvent(locationOfInterest.GetLocation(), distance));
                        }
                    }
                    else
                    {
                        locationsToRemove.Add(entry.Key);
                    }
                }

                // Notify game thread of events, wait for lock if needed.
                if (eventsToReport.Count > 0)
                {
                    Monitor.Enter(this.tracker.Events);
                    this.tracker.Events.AddRange(this.eventsToReport);
                    Monitor.Exit(this.tracker.Events);

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
                    locations.Remove(location);
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
