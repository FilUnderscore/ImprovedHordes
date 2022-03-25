using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using HarmonyLib;

namespace ImprovedHordes.Horde
{
    public sealed class HordeAreaHeatTracker : IManager
    {
        const ulong GameTicksBeforeFull = 168000;
        const ulong GameTicksToFullyDecay = 24000;
        const int EventThreshold = 200;

        private readonly ImprovedHordesManager manager;
        public readonly Dictionary<Vector2i, AreaHeat> chunkHeat = new Dictionary<Vector2i, AreaHeat>();
        public readonly ConcurrentQueue<AreaHeatRequest> queue = new ConcurrentQueue<AreaHeatRequest>();

        private ThreadManager.ThreadInfo threadInfo;
        private ThreadManager.ThreadInfo updateThreadInfo;
        private AutoResetEvent writerThreadWaitHandle = new AutoResetEvent(false);

        public void StartThreads()
        {
            threadInfo = ThreadManager.StartThread("ImprovedHordes-HordeAreaHeatTracker", null, new ThreadManager.ThreadFunctionLoopDelegate(UpdateHeat), null, System.Threading.ThreadPriority.Lowest, _useRealThread: true);
            updateThreadInfo = ThreadManager.StartThread("ImprovedHordes-HordeAreaHeatTrackerDecay", null, new ThreadManager.ThreadFunctionLoopDelegate(UpdateHeatDecay), null, System.Threading.ThreadPriority.Lowest, _useRealThread: true);
        }

        public HordeAreaHeatTracker(ImprovedHordesManager manager)
        {
            this.manager = manager;
        }

        public void Init()
        {
            StartThreads();
        }

        public int UpdateHeat(ThreadManager.ThreadInfo threadInfo)
        {
            while (!threadInfo.TerminationRequested())
            {
                if (queue.Count == 0)
                    writerThreadWaitHandle.WaitOne();

                if(!queue.TryDequeue(out AreaHeatRequest item))
                {
                    Log.Error("Failed to dequeue in Heat Thread.");
                    continue;
                }

                Vector3 position = item.position;
                float value = item.strength;

                foreach (var chunkEntry in GetNearbyChunks(position, 3)) // radius todo
                {
                    var chunk = chunkEntry.Key;
                    var offset = chunkEntry.Value;

                    lock (chunkHeat)
                    {
                        if (!chunkHeat.ContainsKey(chunk))
                            chunkHeat.Add(chunk, new AreaHeat());

                        AreaHeat heat = chunkHeat[chunk];

                        if (heat.IsFull())
                            continue;

                        heat.strength += value * offset;
                        heat.lastUpdate = manager.World.worldTime;
                    }
                }
            }
            
            return -1;
        }

        public int UpdateHeatDecay(ThreadManager.ThreadInfo threadInfo)
        {
            while(!threadInfo.TerminationRequested())
            {
                lock (chunkHeat)
                {
                    foreach (var chunkEntry in chunkHeat)
                    {
                        var chunk = chunkEntry.Key;
                        var heat = chunkEntry.Value;

                        if (heat.strength <= 0.0f)
                            continue;

                        foreach (var player in manager.World.Players.list)
                        {
                            Vector3 playerPosition = player.position;
                            Vector2i playerPosToChunkPos = new Vector2i(global::Utils.Fastfloor(playerPosition.x / 16f), global::Utils.Fastfloor(playerPosition.z / 16f));

                            Vector2i offset = (playerPosToChunkPos - chunk);
                            int sqrMagnitude = offset.x * offset.x + offset.y * offset.y;

                            if (sqrMagnitude <= 3 * 3 * 16) // radius setting todo
                            {
                                break;
                            }

                            heat.strength -= (heat.strength * Mathf.Clamp01((float)(manager.World.worldTime - heat.lastUpdate) / (float)GameTicksToFullyDecay));
                        }
                    }
                }
            }

            return -1;
        }

        public void Tick(ulong worldTime)
        {
            foreach (var player in manager.World.Players.list)
            {
                queue.Enqueue(new AreaHeatRequest(player.position, (float)(1f / GameTicksBeforeFull)));
            }

            this.writerThreadWaitHandle.Set();
        }

        private void NotifyEvent(AIDirectorChunkEvent chunkEvent)
        {
            queue.Enqueue(new AreaHeatRequest(chunkEvent.Position, chunkEvent.Value / EventThreshold));
            this.writerThreadWaitHandle.Set();
        }

        private Dictionary<Vector2i, float> GetNearbyChunks(Vector3 position, int radius)
        {
            int radiusSquared = radius * radius;
            Dictionary<Vector2i, float> nearbyChunks = new Dictionary<Vector2i, float>();

            Vector2i currentChunk = new Vector2i(global::Utils.Fastfloor(position.x / 16f), global::Utils.Fastfloor(position.z / 16f));

            for (int x = -radiusSquared; x <= radiusSquared; x++)
            {
                for (int y = -radiusSquared; y <= radiusSquared; y++)
                {
                    int xDivRad = Math.Abs(x) / radius;
                    int yDivRad = Math.Abs(y) / radius;
                    float strength = (float)(radius - (xDivRad + yDivRad) / 2) / (float)radius;

                    nearbyChunks.Add(new Vector2i(x + currentChunk.x, y + currentChunk.y), strength);
                }
            }

            return nearbyChunks;
        }

        public void Shutdown()
        {
            threadInfo.RequestTermination();
            writerThreadWaitHandle.Set();
            threadInfo.WaitForEnd();
            threadInfo = null;
            writerThreadWaitHandle = null;

            updateThreadInfo.RequestTermination();
            updateThreadInfo.WaitForEnd();
            updateThreadInfo = null;

            chunkHeat.Clear();
        }

        public class AreaHeat
        {
            public float strength;
            public ulong lastUpdate;

            public AreaHeat()
            {
                this.strength = 0.0f;
                this.lastUpdate = ImprovedHordesManager.Instance.World.worldTime;
            }

            public bool IsFull()
            {
                return strength >= 100.0f;
            }

            public override string ToString()
            {
                return $"AreaHeat [strength={strength}, lastUpdate={lastUpdate}]";
            }
        }

        public struct AreaHeatRequest
        {
            public Vector3 position;
            public float strength;

            public AreaHeatRequest(Vector3 position, float strength)
            {
                this.position = position;
                this.strength = strength;
            }
        }

        class HarmonyPatches
        {
            [HarmonyPatch(typeof(AIDirectorChunkEventComponent))]
            [HarmonyPatch("NotifyEvent")]
            class AIDirectorChunkEventComponentNotifyEventHook
            {
                static void Postfix(AIDirectorChunkEvent _chunkEvent)
                {
                    if (!ImprovedHordesMod.IsHost())
                        return;

                    ImprovedHordesManager.Instance.HeatTracker.NotifyEvent(_chunkEvent);
                }
            }
        }
    }
}