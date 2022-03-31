using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using HarmonyLib;
using System;
using ImprovedHordes.Horde.Heat.Events;
using System.IO;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Heat
{
    public sealed class HordeAreaHeatTracker : IManager
    {
        private const ushort HEAT_TRACKER_MAGIC = 0x4854;
        private const uint HEAT_TRACKER_VERSION = 1;

        public const int Radius = 3;
        const ulong GameTicksBeforeFull = 96000;
        const ulong GameTicksToFullyDecay = 48000;
        const ulong GameTicksBeforeDecay = 24000;
        const int EventThreshold = 100;

        private readonly ImprovedHordesManager manager;
        private readonly Dictionary<Vector2i, AreaHeat> chunkHeat = new Dictionary<Vector2i, AreaHeat>();
        private readonly ConcurrentQueue<AreaHeatRequest> queue = new ConcurrentQueue<AreaHeatRequest>();

        private ThreadManager.ThreadInfo threadInfo;
        private AutoResetEvent writerThreadWaitHandle = new AutoResetEvent(false);

        public event EventHandler<AreaHeatTickEvent> OnAreaHeatTick;

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

                Dictionary<Vector2i, float> events = new Dictionary<Vector2i, float>();

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

                        events.Add(chunk, heat.strength);
                    }
                }

                ThreadManager.AddSingleTaskMainThread("ImprovedHordes-HordeAreaHeatTracker.TickEvent", (_param1) =>
                {
                    var areaEvent = OnAreaHeatTick;

                    if (areaEvent == null)
                        return;

                    foreach(var chunkEntry in events)
                    {
                        areaEvent(this, new AreaHeatTickEvent(chunkEntry.Key, chunkEntry.Value));
                    }
                });
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
            return GetHeatInChunk(World.toChunkXZ(position));
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

            Vector2i currentChunk = World.toChunkXZ(position);

            nearbyChunks.Add(currentChunk, 1f);

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

        public void Load(BinaryReader reader)
        {
            if(reader.ReadUInt16() != HEAT_TRACKER_MAGIC || reader.ReadUInt32() < HEAT_TRACKER_VERSION)
            {
                Log("[Heat Tracker] Heat tracker version has changed.");

                return;
            }

            chunkHeat.Clear();
            int chunkHeatSize = reader.ReadInt32();

            for(int i = 0; i < chunkHeatSize; i++)
            {
                Vector2i chunkPosition = new Vector2i(reader.ReadInt32(), reader.ReadInt32());

                float strength = reader.ReadSingle();
                ulong lastUpdateTime = reader.ReadUInt64();

                chunkHeat.Add(chunkPosition, new AreaHeat(strength, lastUpdateTime));
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(HEAT_TRACKER_MAGIC);
            writer.Write(HEAT_TRACKER_VERSION);

            lock (this.chunkHeat)
            {
                writer.Write(this.chunkHeat.Count);

                foreach (var chunkEntry in this.chunkHeat)
                {
                    var key = chunkEntry.Key;
                    var value = chunkEntry.Value;

                    writer.Write(key.x);
                    writer.Write(key.y);

                    writer.Write(value.strength);
                    writer.Write(value.lastUpdate);
                }
            }
        }

        public void Shutdown()
        {
            threadInfo.RequestTermination();
            writerThreadWaitHandle.Set();
            threadInfo.WaitForEnd();
            threadInfo = null;

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

            public AreaHeat(float strength, ulong lastUpdate)
            {
                this.strength = strength;
                this.lastUpdate = lastUpdate;
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