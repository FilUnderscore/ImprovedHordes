using HarmonyLib;
using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Core.World.Event
{
    public sealed class WorldEventReporter : Threaded
    {
        private static readonly Setting<int> EVENT_CHUNK_RADIUS = new Setting<int>("event_chunk_radius", 3);
        private static readonly Setting<float> EVENT_INTEREST_DISTANCE_MULTIPLIER = new Setting<float>("event_interest_distance_multiplier", 0.25f);

        private const double LOG_N_100 = 4.60517018599;
        private readonly double MAP_SIZE_LOG_N, MAP_SIZE_POW_2_LOG_N;

        // Shared
        private readonly ConcurrentQueue<WorldEvent> eventsToStore = new ConcurrentQueue<WorldEvent>();
        private readonly ConcurrentQueue<Vector3> eventsToReportKeys = new ConcurrentQueue<Vector3>();

        // Personal
        private readonly Dictionary<Vector2i, WorldEvent> eventHistory = new Dictionary<Vector2i, WorldEvent>();
        private List<WorldEventReportEvent> eventsToReport = new List<WorldEventReportEvent>();

        private readonly List<Vector2i> eventsToRemove = new List<Vector2i>();

        public event EventHandler<WorldEventReportEvent> OnWorldEventReport;

        public WorldEventReporter(ILoggerFactory loggerFactory, float mapSize) : base(loggerFactory)
        {
            this.MAP_SIZE_LOG_N = Math.Log(mapSize);
            this.MAP_SIZE_POW_2_LOG_N = Math.Pow(2, MAP_SIZE_LOG_N);

            AIDirectorChunkEventComponent_NotifyEvent_Patch.WorldEventReporter = this;
            AIDirectorMarkerManagementComponent_NotifyNoise_Patch.WorldEventReporter = this;
        }

        protected override void UpdateAsync(float dt)
        {
            while (eventsToStore.TryDequeue(out WorldEvent worldEvent))
            {
                if (eventHistory.TryGetValue(worldEvent.GetChunkLocation(), out WorldEvent chunkHistoryEvent))
                {
                    chunkHistoryEvent.Add(worldEvent);
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
                if(eventHistory.TryGetValue(global::World.toChunkXZ(key), out WorldEvent worldEvent))
                {
                    float interest = worldEvent.GetInterestLevel();
                    eventsToReport.Add(new WorldEventReportEvent(key, interest, CalculateInterestDistance(interest)));
                }
            }

            if (this.OnWorldEventReport != null)
            {
                foreach (var eventToReport in eventsToReport)
                {
                    this.OnWorldEventReport.Invoke(this, eventToReport);
                }
            }

            eventsToReport.Clear();
        }

        public void Report(WorldEvent worldEvent)
        {
            Task.Run(() =>
            {
                float interest = worldEvent.GetInterestLevel();

                ConcurrentDictionary<Vector2i, float> nearby = GetNearbyChunks(worldEvent.GetLocation(), EVENT_CHUNK_RADIUS.Value);

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

            return (int)(distance * EVENT_INTEREST_DISTANCE_MULTIPLIER.Value);
        }

        [HarmonyPatch(typeof(AIDirectorChunkEventComponent))]
        [HarmonyPatch("NotifyEvent")]
        private class AIDirectorChunkEventComponent_NotifyEvent_Patch
        {
            public static WorldEventReporter WorldEventReporter;

            static void Postfix(AIDirectorChunkEvent _chunkEvent)
            {
                if (_chunkEvent.EventType == EnumAIDirectorChunkEvent.Sound || _chunkEvent.Value <= float.Epsilon)
                    return;

                WorldEventReporter.Report(new WorldEvent(_chunkEvent.Position, _chunkEvent.Value * 10));
            }
        }

        [HarmonyPatch(typeof(AIDirectorMarkerManagementComponent))]
        [HarmonyPatch(nameof(AIDirectorMarkerManagementComponent.NotifyNoise))]
        private sealed class AIDirectorMarkerManagementComponent_NotifyNoise_Patch
        {
            public static WorldEventReporter WorldEventReporter;

            private static void Postfix(AIDirectorMarkerManagementComponent __instance, Entity instigator, Vector3 position, string clipName, float volumeScale)
            {
                if (instigator == null || string.IsNullOrEmpty(clipName) || instigator.IsIgnoredByAI())
                    return;

                if (!AIDirectorData.FindNoise(clipName, out var noise))
                    return;

                var trackedPlayers = __instance.Director.GetComponent<AIDirectorPlayerManagementComponent>().trackedPlayers;

                if (!trackedPlayers.dict.TryGetValue(instigator.entityId, out var directorPlayerState) || directorPlayerState == null)
                    return;

                float strength = 1.0f * (directorPlayerState.Player.Stealth.noiseVolume / 100.0f);

                if (directorPlayerState.Player.IsCrouching)
                {
                    strength *= noise.muffledWhenCrouched;
                    volumeScale *= strength;
                }

                float interest = noise.heatMapStrength * volumeScale * (10 * strength);

                if (interest <= float.Epsilon)
                    return;

                WorldEventReporter.Report(new WorldEvent(global::World.worldToBlockPos(position), interest, strength));
            }
        }
    }
}
