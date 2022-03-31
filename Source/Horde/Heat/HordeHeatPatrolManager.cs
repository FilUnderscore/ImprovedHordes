using ImprovedHordes.Horde.Heat.Events;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Horde.Heat
{
    public class HordeHeatPatrolManager : IManager
    {
        private Dictionary<Vector2i, ulong> patrolTime = new Dictionary<Vector2i, ulong>();
        private readonly ImprovedHordesManager manager;
        private readonly PatrolHordeSpawner spawner;

        public HordeHeatPatrolManager(ImprovedHordesManager manager)
        {
            this.manager = manager;
            this.manager.HeatTracker.OnAreaHeatTick += OnAreaHeatTick;

            this.spawner = new PatrolHordeSpawner(manager);
        }

        private void OnAreaHeatTick(object sender, AreaHeatTickEvent e)
        {
            Vector2i area = GetAreaFromChunk(e.chunk);
            
            if (patrolTime.ContainsKey(area))
            {
                if (manager.World.worldTime >= patrolTime[area])
                {
                    Vector3 pos = new Vector3(e.chunk.x * 16f, 0, e.chunk.y * 16f);
                    Utils.GetSpawnableY(ref pos);

                    this.spawner.StartSpawningFor(this.spawner.GetHordeGroupNearLocation(pos), false);
                    patrolTime.Remove(area);
                }

                return;
            }

            CalculatePatrolTime(e.chunk, e.heat);
        }

        public Vector2i GetAreaFromChunk(Vector2i chunk)
        {
            return new Vector2i(chunk.x / (HordeAreaHeatTracker.Radius * HordeAreaHeatTracker.Radius), chunk.y / (HordeAreaHeatTracker.Radius * HordeAreaHeatTracker.Radius));
        }

        public void Update()
        {
            if (!manager.PlayerManager.AnyPlayers())
                return;

            this.spawner.Update();
        }

        private void CalculatePatrolTime(Vector2i chunk, float heat)
        {
            Vector3i position = new Vector3i(chunk.x * 16, 0, chunk.y * 16);
            BiomeDefinition def = manager.World.GetBiome(position.x, position.z);

            float mod = def != null ? def.LootStageMod : 0f;
            float difficulty = mod + 1f + (heat / 100f);

            ulong worldTime = manager.World.worldTime;
            ulong time = (ulong)(24000f / difficulty);

            Log.Out("Time: " + GameUtils.WorldTimeToString(worldTime + time) + " Def NULL: " + (def == null));

            Log.Out("Chunk: " + chunk);
            Log.Out("Area: " + GetAreaFromChunk(chunk));
            patrolTime.Add(GetAreaFromChunk(chunk), worldTime + time);
        }

        public void Shutdown()
        {
            patrolTime.Clear();
        }
    }
}