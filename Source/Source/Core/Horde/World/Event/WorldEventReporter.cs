using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Event
{
    public sealed class WorldEventReporter
    {
        private const int EVENT_CHUNK_RADIUS = 3;

        private const double LOG_N_100 = 4.60517018599;
        private readonly double MAP_SIZE_LOG_N, MAP_SIZE_POW_2_LOG_N;

        // Shared
        private readonly ConcurrentQueue<WorldEvent> eventsToStore = new ConcurrentQueue<WorldEvent>();
        private readonly ConcurrentQueue<Vector3> eventsToReportKeys = new ConcurrentQueue<Vector3>();

        // Personal
        private readonly Dictionary<Vector2i, WorldEvent> eventHistory = new Dictionary<Vector2i, WorldEvent>();
        private List<WorldEventReportEvent> eventsToReport = new List<WorldEventReportEvent>();

        private readonly List<Vector2i> eventsToRemove = new List<Vector2i>();

        private Task UpdateTask;

        public event EventHandler<WorldEventReportEvent> OnWorldEventReport;

        public WorldEventReporter(float mapSize)
        {
            Log.Out("[World Event Reporter] Map Size: " + mapSize);

            this.MAP_SIZE_LOG_N = Math.Log(mapSize);
            this.MAP_SIZE_POW_2_LOG_N = Math.Pow(2, MAP_SIZE_LOG_N);

            AIDirectorChunkEventComponent_NotifyEvent_Patch.WorldEventReporter = this;
        }

        public void Update()
        {
            if(UpdateTask != null && UpdateTask.IsCompleted)
            {
                if (this.OnWorldEventReport != null)
                {
                    foreach (var eventToReport in eventsToReport)
                    {
                        this.OnWorldEventReport.Invoke(this, eventToReport);
                    }
                }

                eventsToReport.Clear();
            }

            if(UpdateTask == null || UpdateTask.IsCompleted)
            {
                this.UpdateTask = Task.Run(() =>
                {
                    while(eventsToStore.TryDequeue(out WorldEvent worldEvent))
                    {
                        if(eventHistory.ContainsKey(worldEvent.GetChunkLocation()))
                        {
                            eventHistory[worldEvent.GetChunkLocation()].Add(worldEvent);
                        }
                        else
                        {
                            eventHistory.Add(worldEvent.GetChunkLocation(), worldEvent);
                        }
                    }

                    foreach (var worldEventEntry in eventHistory)
                    {
                        if (worldEventEntry.Value.HasLostInterest())
                        {
                            eventsToRemove.Add(worldEventEntry.Key);
                        }
                    }

                    foreach (var eventToRemove in eventsToRemove)
                    {
                        this.eventHistory.Remove(eventToRemove);
                    }
                    eventsToRemove.Clear();

                    while (eventsToReportKeys.TryDequeue(out Vector3 key))
                    {
                        WorldEvent worldEvent = eventHistory[global::World.toChunkXZ(key)];
                        float interest = worldEvent.GetInterestLevel();

                        eventsToReport.Add(new WorldEventReportEvent(key, interest, CalculateInterestDistance(interest)));
                    }
                });
            }
        }

        public void Report(WorldEvent worldEvent)
        {
            Task.Run(() =>
            {
                float interest = worldEvent.GetInterestLevel();

                ConcurrentDictionary<Vector2i, float> nearby = GetNearbyChunks(worldEvent.GetLocation(), EVENT_CHUNK_RADIUS);

                foreach(var entry in nearby)
                {
                    eventsToStore.Enqueue(new WorldEvent(worldEvent.GetLocation(), entry.Key, interest * entry.Value, entry.Value));
                }

                eventsToStore.Enqueue(worldEvent);
                eventsToReportKeys.Enqueue(worldEvent.GetLocation());
            });
        }

        private ConcurrentDictionary<Vector2i, float> GetNearbyChunks(Vector3 position, int radius)
        {
            int radiusSquared = radius * radius;
            ConcurrentDictionary<Vector2i, float> nearbyChunks = new ConcurrentDictionary<Vector2i, float>();

            Vector2i currentChunk = global::World.toChunkXZ(position);

            Parallel.For(1, radiusSquared + 1, x =>
            {
                float xDivRad = (float)(x / radius) / (float)radius;
                float strengthX = 1f - xDivRad;

                Parallel.For(1, radiusSquared + 1, y =>
                {
                    float yDivRad = (float)(y / radius) / (float)radius;
                    float strengthY = 1f - yDivRad;

                    float strength = (strengthX + strengthY) / 2f;

                    nearbyChunks.TryAdd(new Vector2i(currentChunk.x + x, currentChunk.y + y), strength);
                    nearbyChunks.TryAdd(new Vector2i(currentChunk.x - x, currentChunk.y - y), strength);
                    nearbyChunks.TryAdd(new Vector2i(currentChunk.x + x, currentChunk.y - y), strength);
                    nearbyChunks.TryAdd(new Vector2i(currentChunk.x - x, currentChunk.y + y), strength);
                });

                nearbyChunks.TryAdd(new Vector2i(currentChunk.x + x, currentChunk.y), strengthX);
                nearbyChunks.TryAdd(new Vector2i(currentChunk.x - x, currentChunk.y), strengthX);
                nearbyChunks.TryAdd(new Vector2i(currentChunk.x, currentChunk.y + x), strengthX);
                nearbyChunks.TryAdd(new Vector2i(currentChunk.x, currentChunk.y - x), strengthX);
            });

            return nearbyChunks;
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

        [HarmonyPatch(typeof(AIDirectorChunkEventComponent))]
        [HarmonyPatch("NotifyEvent")]
        private class AIDirectorChunkEventComponent_NotifyEvent_Patch
        {
            public static WorldEventReporter WorldEventReporter;

            static void Postfix(AIDirectorChunkEvent _chunkEvent)
            {
                WorldEventReporter.Report(new WorldEvent(_chunkEvent.Position, _chunkEvent.Value * 5));
            }
        }
    }
}
