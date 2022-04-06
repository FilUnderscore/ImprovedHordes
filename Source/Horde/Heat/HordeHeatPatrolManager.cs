using ImprovedHordes.Horde.Heat.Events;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using static ImprovedHordes.Utils.Logger;
using ImprovedHordes.Horde.AI.Events;

namespace ImprovedHordes.Horde.Heat
{
    public class HordeHeatPatrolManager : IManager
    {
        private const ushort HEAT_PATROL_MAGIC = 0x4850;
        private const uint HEAT_PATROL_VERSION = 1;

        private Dictionary<Vector2i, ulong> patrolTime = new Dictionary<Vector2i, ulong>();
        private readonly ImprovedHordesManager manager;
        private readonly PatrolHordeSpawner spawner;

        public HordeHeatPatrolManager(ImprovedHordesManager manager)
        {
            this.manager = manager;
            this.manager.HeatTracker.OnAreaHeatTick += OnAreaHeatTick;
            this.manager.AIManager.OnHordeKilled += OnHordeKilled;

            this.spawner = new PatrolHordeSpawner(manager);
        }

        private void OnAreaHeatTick(object sender, AreaHeatTickEvent e)
        {
            Vector2i area = GetAreaFromChunk(e.chunk);

            if (patrolTime.ContainsKey(area))
            {
                if (manager.World.worldTime >= patrolTime[area])
                {
                    Vector2 center = GetCenterOfArea(area);
                    Vector3 pos = new Vector3(center.x, 0, center.y);
                    Utils.GetSpawnableY(ref pos);

                    PlayerHordeGroup group = this.spawner.GetHordeGroupNearLocation(pos);
                    
                    if (group != null && group.members.Count > 0)
                    {
                        this.spawner.StartSpawningFor(group, false);
                    }

                    patrolTime.Remove(area);
                }
            }
            else
            {
                CalculatePatrolTime(e.chunk, e.heat);
            }
        }

        private void OnHordeKilled(object sender, HordeKilledEvent e)
        {
            if (!e.horde.GetHordeInstance().group.list.type.EqualsCaseInsensitive("Patrol"))
                return;

            if(e.horde.GetStat(AI.EHordeAIStats.TOTAL_KILLED) == 0)
            {
                Vector3 location = e.horde.GetHordeInstance().playerGroup.CalculateAverageGroupPosition(false);
                float heat = Mathf.Clamp(manager.HeatTracker.GetHeatAt(location), 0, 20f);

                manager.HeatTracker.Request(location, -(heat / ((float)HordeAreaHeatTracker.HRS_BEFORE_FULL / (float)(HordeAreaHeatTracker.HRS_BEFORE_DECAY + HordeAreaHeatTracker.HRS_TO_FULLY_DECAY))));
            }
        }

        public Vector2i GetAreaFromChunk(Vector2i chunk)
        {
            int radiusSquared = HordeAreaHeatTracker.RADIUS_SQUARED;
            
            return new Vector2i(chunk.x / (2 * radiusSquared), chunk.y / (2 * radiusSquared));
        }

        public Vector2 GetCenterOfArea(Vector2i area)
        {
            int radiusSquared = HordeAreaHeatTracker.RADIUS_SQUARED;

            return new Vector2(16f * area.x * radiusSquared, 16f * area.y * radiusSquared);
        }

        public bool GetAreaPatrolTime(Vector3 position, out ulong time)
        {
            Vector2i area = GetAreaFromChunk(World.toChunkXZ(position));

            if (patrolTime.ContainsKey(area))
            {
                time = patrolTime[area];
                return true;
            }

            time = 0;
            return false;
        }

        public void Update()
        {
            if (!manager.PlayerManager.AnyPlayers())
                return;

            this.spawner.Update();
        }

        public void Load(BinaryReader reader)
        {
            if(reader.ReadUInt16() != HEAT_PATROL_MAGIC || reader.ReadUInt32() < HEAT_PATROL_VERSION)
            {
                Log("[Heat Patrol] Heat patrol version has changed.");

                return;
            }

            patrolTime.Clear();
            int patrolTimeSize = reader.ReadInt32();

            for(int i = 0; i < patrolTimeSize; i++)
            {
                Vector2i areaPosition = new Vector2i(reader.ReadInt32(), reader.ReadInt32());

                ulong time = reader.ReadUInt64();

                patrolTime.Add(areaPosition, time);
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(HEAT_PATROL_MAGIC);
            writer.Write(HEAT_PATROL_VERSION);

            writer.Write(this.patrolTime.Count);

            foreach(var patrolEntry in this.patrolTime)
            {
                var key = patrolEntry.Key;
                var value = patrolEntry.Value;

                writer.Write(key.x);
                writer.Write(key.y);

                writer.Write(value);
            }
        }

        private void CalculatePatrolTime(Vector2i chunk, float heat)
        {
            Vector2i area = GetAreaFromChunk(chunk);
            Vector2 center = GetCenterOfArea(area);
            Vector2i position = new Vector2i(center);
            BiomeDefinition def = manager.World.GetBiome(position.x, position.y);

            if (def == null)
            {
                def = manager.World.GetBiome(global::Utils.Fastfloor(chunk.x * 16f), global::Utils.Fastfloor(chunk.y * 16f));

                if (def == null)
                    return;
            }

            float mod = def.LootStageMod;
            float difficulty = mod * mod + 1f + (heat / 100f) * (mod + 1f);

            ulong worldTime = manager.World.worldTime;
            ulong time = (ulong)(24000f / difficulty);

            patrolTime.Add(area, worldTime + time);
        }

        public void Shutdown()
        {
            patrolTime.Clear();
        }
    }
}