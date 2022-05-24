using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using HarmonyLib;
using System;
using ImprovedHordes.Horde.Heat.Events;
using System.IO;

using CustomModManager.API;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Heat
{
    public sealed class HordeAreaHeatTracker : IManager
    {
        private const ushort HEAT_TRACKER_MAGIC = 0x4854;
        private const uint HEAT_TRACKER_VERSION = 1;

        private static bool s_enabled = true;
        private static int s_radius = 3, s_radius_squared = s_radius * s_radius;
        private static int s_hrs_before_full = 168, s_hrs_before_decay = 24, s_hrs_to_fully_decay = 96;
        private static float s_event_multiplier = 0.01f;

        public static bool ENABLED
        {
            get
            {
                return s_enabled;
            }
        }

        public static int RADIUS
        {
            get
            {
                return s_radius;
            }
        }

        public static int RADIUS_SQUARED
        {
            get
            {
                return s_radius_squared;
            }
        }

        public static int HRS_BEFORE_FULL
        {
            get
            {
                return s_hrs_before_full;
            }
        }

        public static int HRS_BEFORE_DECAY
        {
            get
            {
                return s_hrs_before_decay;
            }
        }

        public static int HRS_TO_FULLY_DECAY
        {
            get
            {
                return s_hrs_to_fully_decay;
            }
        }

        public static float EVENT_MULTIPLIER
        {
            get
            {
                return s_event_multiplier;
            }
        }

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

        public void ReadSettings(Settings settings)
        {
            s_enabled = settings.GetBool("enabled", true);
            s_radius = settings.GetInt("radius", 0, false, 3);
            s_radius_squared = s_radius * s_radius;
            s_hrs_before_full = settings.GetInt("hrs_before_full", 0, false, 168);
            s_hrs_before_decay = settings.GetInt("hrs_before_decay", 1, false, 24);
            s_hrs_to_fully_decay = settings.GetInt("hrs_to_fully_decay", 0, false, 96);
            s_event_multiplier = settings.GetFloat("event_multiplier", 0f, false, 0.01f);
        }

        public void HookSettings(ModManagerAPI.ModSettings modSettings)
        {
            modSettings.Hook("hordeAreaHeatTrackerEnabled", "IHxuiHordeAreaHeatTrackerEnabled", value => s_enabled = value, () => s_enabled, toStr => (toStr.ToString(), toStr.ToString()), str =>
            {
                bool success = bool.TryParse(str, out bool val);
                return (val, success);
            }).SetTab("heatTrackerSettingsTab").SetAllowedValues(new bool[] { true, false });

            modSettings.Hook("radius", "IHxuiHeatRadiusModSetting", value => s_radius = value, () => s_radius, toStr => (toStr.ToString(), toStr.ToString() + " Chunk" + (toStr > 1 ? "s" : "")), str =>
            {
                bool success = int.TryParse(str, out int val);
                return (val, success);
            }).SetTab("heatTrackerSettingsTab").SetMinimumMaximumAndIncrementValues(1, GamePrefs.GetInt(EnumGamePrefs.OptionsGfxViewDistance), 1);

            modSettings.Hook("hrs_before_full", "IHxuiHrsBeforeFullModSetting", value => s_hrs_before_full = value, () => s_hrs_before_full, toStr => (toStr.ToString(), toStr.ToString() + " Hour" + (toStr > 1 ? "s" : "")), str =>
            {
                bool success = int.TryParse(str, out int val) && val > 0;
                return (val, success);
            }).SetTab("heatTrackerSettingsTab");

            modSettings.Hook("hrs_before_decay", "IHxuiHrsBeforeDecayModSetting", value => s_hrs_before_decay = value, () => s_hrs_before_decay, toStr => (toStr.ToString(), toStr.ToString() + " Hour" + (toStr > 1 ? "s" : "")), str =>
            {
                bool success = int.TryParse(str, out int val) && val > 0;
                return (val, success);
            }).SetTab("heatTrackerSettingsTab");

            modSettings.Hook("hrs_to_fully_decay", "IHxuiHrsToFullyDecayModSetting", value => s_hrs_to_fully_decay = value, () => s_hrs_to_fully_decay, toStr => (toStr.ToString(), toStr.ToString() + " Hour" + (toStr > 1 ? "s" : "")), str =>
            {
                bool success = int.TryParse(str, out int val) && val > 0;
                return (val, success);
            }).SetTab("heatTrackerSettingsTab");

            modSettings.Hook("event_multiplier", "IHxuiEventMultiplierModSetting", value => s_event_multiplier = value, () => s_event_multiplier, toStr => (toStr.ToString(), toStr.ToString() + "x"), str =>
            {
                bool success = float.TryParse(str, out float val) && val >= 0.0f;
                return (val, success);
            }).SetTab("heatTrackerSettingsTab");
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

                var areaEvent = OnAreaHeatTick;

                foreach (var chunkEntry in GetNearbyChunks(position, RADIUS))
                {
                    var chunk = chunkEntry.Key;
                    var offset = chunkEntry.Value;

                    lock (chunkHeat)
                    {
                        if (!chunkHeat.ContainsKey(chunk))
                            chunkHeat.Add(chunk, new AreaHeat());

                        AreaHeat heat = chunkHeat[chunk];

                        float decay = Mathf.Clamp(heat.WasUnloaded(worldTime) ? 100.0f * Mathf.Clamp01((float)(worldTime - heat.lastUpdate) / (float)(HRS_TO_FULLY_DECAY * 1000)) : 0f, 0f, 100f);
                        float gain = Mathf.Clamp(heat.strength + (!heat.IsFull() ? value * offset : 0.0f), 0f, 100f);

                        heat.strength = gain - decay;
                        heat.lastUpdate = manager.World.worldTime;

                        events.Add(chunk, heat.strength);
                    }
                }

                if (areaEvent != null)
                {
                    foreach (var chunkEntry in events)
                    {
                        areaEvent(this, new AreaHeatTickEvent(chunkEntry.Key, chunkEntry.Value));
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
                queue.Enqueue(new AreaHeatRequest(player.position, (float)(100f / (HRS_BEFORE_FULL * 1000))));
            }

            this.writerThreadWaitHandle.Set();
        }

        private void NotifyEvent(AIDirectorChunkEvent chunkEvent)
        {
            Request(chunkEvent.Position, chunkEvent.Value * EVENT_MULTIPLIER);
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
                nearbyChunks.Add(new Vector2i(currentChunk.x, currentChunk.y + x), strengthX);
                nearbyChunks.Add(new Vector2i(currentChunk.x, currentChunk.y - x), strengthX);
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
                return worldTime - lastUpdate >= ((ulong)HRS_BEFORE_DECAY * 1000UL);
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