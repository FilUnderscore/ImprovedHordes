using HarmonyLib;
using ImprovedHordes.Source.Horde.World.LOI;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ImprovedHordes.Source.Core.Horde.World.LOI
{
    public sealed partial class WorldLOITracker
    {
        private readonly LOIAreaImpactor impactor;
        private readonly LOIInterestDecayer decayer;

        // Private
        private readonly List<LocationOfInterest> toReport = new List<LocationOfInterest>();
        private readonly object ReportLock = new object();

        // Shared
        private readonly List<LOIInterestNotificationEvent> Events = new List<LOIInterestNotificationEvent>();
        private readonly object EventsLock = new object();

        public event EventHandler<LOIInterestNotificationEvent> OnInterestNotificationMainThread;
        public event EventHandler<LOIInterestNotificationEvent> OnInterestNotificationEventThread;

        public WorldLOITracker(float mapSize)
        {
            Log.Out($"[WorldLOITracker] Map Size is {mapSize}");

            this.decayer = new LOIInterestDecayer(this, mapSize);
            this.impactor = new LOIAreaImpactor(this.decayer);
            
            this.decayer.ExecuteThread();
            this.impactor.ExecuteThread();

            HarmonyPatches.AIDirectorChunkEventComponent_NotifyEvent_Hook.WorldLOITracker = this;
            this.OnInterestNotificationMainThread += WorldLOITracker_OnInterestNotificationMainThread;
        }

        private void WorldLOITracker_OnInterestNotificationMainThread(object sender, LOIInterestNotificationEvent e)
        {
            //Log.Out($"Event: {e.GetLocation()}: {e.GetDistance()} blocks");
        }

        private void Report(LocationOfInterest location)
        {
            Monitor.Enter(ReportLock);
            this.toReport.Add(location);
            Monitor.Exit(ReportLock);
        }

        public void Shutdown()
        {
            this.decayer.ShutdownThread();
            this.decayer.Notify(null);

            this.impactor.ShutdownThread();
            this.impactor.Notify(null);
        }

        public void Update()
        {
            this.TryReport();
            this.NotifyEventsOnMainThread();
        }

        private void TryReport()
        {
            if (Monitor.TryEnter(ReportLock))
            {
                if (this.impactor.Notify(this.toReport))
                {
                    this.toReport.Clear();
                }

                Monitor.Exit(ReportLock);
            }
        }

        private void NotifyEventsOnMainThread()
        {
            if (OnInterestNotificationMainThread == null)
                return;

            // Try acquire events if written to.
            if (Monitor.TryEnter(EventsLock))
            {
                if (Events.Count > 0)
                {
                    foreach (LOIInterestNotificationEvent notificationEvent in this.Events)
                    {
                        OnInterestNotificationMainThread.Invoke(this, notificationEvent);
                    }

                    this.Events.Clear();
                }

                Monitor.Exit(EventsLock);
            }
        }

        private class HarmonyPatches
        {
            [HarmonyPatch(typeof(AIDirectorChunkEventComponent))]
            [HarmonyPatch("NotifyEvent")]
            public class AIDirectorChunkEventComponent_NotifyEvent_Hook
            {
                public static WorldLOITracker WorldLOITracker;

                static void Postfix(AIDirectorChunkEvent _chunkEvent)
                {
                    if (WorldLOITracker == null)
                        return;

                    WorldLOITracker.Report(new ChunkEventLOI(_chunkEvent));
                }

                private class ChunkEventLOI : LocationOfInterest
                {
                    public ChunkEventLOI(AIDirectorChunkEvent chunkEvent) : base(chunkEvent.Position, chunkEvent.Value * 10, 1.0f)
                    {
                    }
                }
            }
        }
    }
}