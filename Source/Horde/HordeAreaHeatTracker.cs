using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using HarmonyLib;

namespace ImprovedHordes.Horde
{
    public sealed class HordeAreaHeatTracker : IManager
    {
        const ulong GameTicksBeforeFull = 96000;
        const ulong GameTicksToFullyDecay = 48000;
        const ulong GameTicksBeforeDecay = 24000;
        const int EventThreshold = 100;

        private readonly ImprovedHordesManager manager;
        private readonly Dictionary<Vector2i, AreaHeat> chunkHeat = new Dictionary<Vector2i, AreaHeat>();
        private readonly ConcurrentQueue<AreaHeatRequest> queue = new ConcurrentQueue<AreaHeatRequest>();

        private ThreadManager.ThreadInfo threadInfo;
        private AutoResetEvent writerThreadWaitHandle = new AutoResetEvent(false);

        public void StartThreads()
        {
            threadInfo = ThreadManager.StartThread("ImprovedHordes-HordeAreaHeatTracker", null, new ThreadManager.ThreadFunctionLoopDelegate(UpdateHeat), null, System.Threading.ThreadPriority.Lowest, _useRealThread: true);
        }

        public HordeAreaHeatTracker(ImprovedHordesManager manager)
        {
            this.manager = manager;
        }

        public void Init()
        {
            StartThreads();
        }

        private int UpdateHeat(ThreadManager.ThreadInfo threadInfo)
        {
            while (!threadInfo.TerminationRequested())
            {
                if (queue.Count == 0)
                    writerThreadWaitHandle.WaitOne();

                if(!queue.TryDequeue(out AreaHeatRequest item))
                {
                    continue;
                }

                Vector3 position = item.position;
                float value = item.strength;

                ulong worldTime = manager.World.worldTime;

                foreach (var chunkEntry in GetNearbyChunks(position, 3)) // radius todo
                {
                    var chunk = chunkEntry.Key;
                    var offset = chunkEntry.Value;

                    lock (chunkHeat)
                    {
                        if (!chunkHeat.ContainsKey(chunk))
                            chunkHeat.Add(chunk, new AreaHeat());

                        AreaHeat heat = chunkHeat[chunk];

                        heat.strength += (!heat.IsFull() ? value * offset : 0.0f) - (heat.WasUnloaded(worldTime) ? (heat.strength * Mathf.Clamp01((float)(worldTime - heat.lastUpdate) / (float)GameTicksToFullyDecay)) : 0);
                        heat.lastUpdate = manager.World.worldTime;
                    }
                }
            }
            
            return -1;
        }

        public float GetHeatInChunk(Vector2i chunkPos)
        {
            lock (chunkHeat)
            {
                if (!chunkHeat.ContainsKey(chunkPos))
                    return 0.0f;

                return chunkHeat[chunkPos].strength;
            }
        }

        public float GetHeatAt(Vector3 position)
        {
            return GetHeatInChunk(new Vector2i(global::Utils.Fastfloor(position.x / 16), global::Utils.Fastfloor(position.z / 16)));
        }

        public float GetHeatForGroup(PlayerHordeGroup group)
        {
            return GetHeatAt(group.CalculateAverageGroupPosition(false));
        }

        public void Request(Vector3 position, float value)
        {
            queue.Enqueue(new AreaHeatRequest(position, value));
            this.writerThreadWaitHandle.Set();
        }

        public void Tick(ulong worldTime)
        {
            foreach (var player in manager.World.Players.list)
            {
                queue.Enqueue(new AreaHeatRequest(player.position, (float)(100f / GameTicksBeforeFull)));
            }

            this.writerThreadWaitHandle.Set();
        }

        private void NotifyEvent(AIDirectorChunkEvent chunkEvent)
        {
            Request(chunkEvent.Position, chunkEvent.Value / EventThreshold);
        }

        private Dictionary<Vector2i, float> GetNearbyChunks(Vector3 position, int radius)
        {
            int radiusSquared = radius * radius;
            Dictionary<Vector2i, float> nearbyChunks = new Dictionary<Vector2i, float>();

            Vector2i currentChunk = new Vector2i(global::Utils.Fastfloor(position.x / 16f), global::Utils.Fastfloor(position.z / 16f));

            nearbyChunks.Add(new Vector2i(currentChunk.x, currentChunk.y), 1f);

            for (int x = 1; x <= radiusSquared; x++)
            {
                float xDivRad = (float)(x / radius) / (float)radius;
                float strengthX = 1f - xDivRad;

                for (int y = 1; y <= radiusSquared; y++)
                {
                    float yDivRad = (float)(y / radius) / (float)radius;
                    float strengthY = 1f - yDivRad;

                    float strength = (strengthX + strengthY) / 2f;

                    nearbyChunks.Add(new Vector2i(currentChunk.x + x, currentChunk.y + y), strength);
                    nearbyChunks.Add(new Vector2i(currentChunk.x - x, currentChunk.y - y), strength);
                    nearbyChunks.Add(new Vector2i(currentChunk.x + x, currentChunk.y - y), strength);
                    nearbyChunks.Add(new Vector2i(currentChunk.x - x, currentChunk.y + y), strength);
                }

                nearbyChunks.Add(new Vector2i(currentChunk.x + x, currentChunk.y), strengthX);
                nearbyChunks.Add(new Vector2i(currentChunk.x - x, currentChunk.y), strengthX);
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

            chunkHeat.Clear();
        }

        private class AreaHeat
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

            public bool WasUnloaded(ulong worldTime)
            {
                return worldTime - lastUpdate >= GameTicksBeforeDecay;
            }

            public override string ToString()
            {
                return $"AreaHeat [strength={strength}, lastUpdate={lastUpdate}]";
            }
        }

        private struct AreaHeatRequest
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