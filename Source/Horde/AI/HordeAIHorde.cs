using System.Collections.Generic;
using System.Reflection;
using System;

using HarmonyLib;

using ImprovedHordes.Horde.AI.Events;

namespace ImprovedHordes.Horde.AI
{
    public class HordeAIHorde
    {
        private static readonly MethodInfo DespawnMethod = AccessTools.Method(typeof(EntityAlive), "Despawn");
        
        static HordeAIHorde()
        {
            if (DespawnMethod == null)
                throw new NullReferenceException($"{nameof(DespawnMethod)} is null.");
        }

        private readonly Horde horde;
        private readonly Dictionary<int, HordeAIEntity> entities = new Dictionary<int, HordeAIEntity>();
        private readonly Dictionary<EHordeAIStats, int> stats = new Dictionary<EHordeAIStats, int>();

        public event EventHandler<HordeEntityKilledEvent> OnHordeEntityKilled;
        public event EventHandler<HordeEntityDespawnedEvent> OnHordeEntityDespawned;

        public HordeAIHorde(Horde horde)
        {
            this.horde = horde;
        }

        public Horde GetHordeInstance()
        {
            return this.horde;
        }

        public void AddEntity(EntityAlive entity, bool despawnOnCompletion, List<HordeAICommand> commands)
        {
            HordeAIEntity aiEntity = new HordeAIEntity(entity, despawnOnCompletion, commands);
            this.AddEntity(aiEntity);

            ImprovedHordesManager.Instance.AIManager.OnHordeAIEntitySpawnedEvent(aiEntity, this);
        }

        private void AddEntity(HordeAIEntity entity)
        {
            if (entities.ContainsKey(entity.GetEntityId()))
                return;

            entities.Add(entity.GetEntityId(), entity);
            IncrementStat(EHordeAIStats.TOTAL_SPAWNED);
            IncrementStat(EHordeAIStats.TOTAL_ALIVE);
        }

        private void IncrementStat(EHordeAIStats stat)
        {
            if (!stats.ContainsKey(stat))
                stats.Add(stat, 0);

            stats[stat]++;
        }

        private void DecrementStat(EHordeAIStats stat)
        {
            if (stats.ContainsKey(stat))
                stats[stat]--;
        }

        public int GetStat(EHordeAIStats stat)
        {
            if (!stats.ContainsKey(stat))
                return 0;

            return stats[stat];
        }

        public HordeAIEntity GetEntity(int entityId)
        {
            if (!entities.ContainsKey(entityId))
                return null;

            return entities[entityId];
        }

        private bool disbanded = false;

        public void Disband()
        {
            disbanded = true;
            this.entities.Clear();
        }

        public int GetAlive()
        {
            return this.entities.Count;
        }

        private readonly List<HordeAIEntity> toRemove = new List<HordeAIEntity>();
        public EHordeAIHordeUpdateState Update(float dt)
        {
            foreach(var entity in this.entities.Values)
            {
                EHordeAIEntityUpdateState entityUpdateState = entity.Update(dt);

                switch(entityUpdateState)
                {
                    case EHordeAIEntityUpdateState.CONTINUE_COMMAND:
                        continue;
                    case EHordeAIEntityUpdateState.NEXT_COMMAND:
                        entity.currentCommandIndex++; // Increment command index by 1.
                        break;
                    case EHordeAIEntityUpdateState.FINISHED:
                    case EHordeAIEntityUpdateState.DEAD:
                        if (entityUpdateState == EHordeAIEntityUpdateState.FINISHED)
                        {
                            if(!entity.despawnOnCompletion)
                            {
                                if (entity.entity is EntityEnemy enemy)
                                    enemy.IsHordeZombie = false;

                                entity.entity.bIsChunkObserver = false;
                            }
                            else
                            {
                                DespawnMethod.Invoke(entity.entity, new object[0]);

                                IncrementStat(EHordeAIStats.TOTAL_DESPAWNED);
                                OnHordeEntityDespawnedEvent(entity);
                            }
                        }
                        else
                        {
                            IncrementStat(EHordeAIStats.TOTAL_KILLED);
                            OnHordeEntityKilledEvent(entity);
                        }

                        DecrementStat(EHordeAIStats.TOTAL_ALIVE);

                        toRemove.Add(entity);
                        break;
                }
            }

            foreach(var entity in toRemove)
            {
                this.entities.Remove(entity.GetEntityId());
            }

            if (toRemove.Count > 0)
                toRemove.Clear();

            return disbanded || (GetStat(EHordeAIStats.TOTAL_ALIVE) == 0 && GetStat(EHordeAIStats.TOTAL_SPAWNED) == GetHordeInstance().count) ? EHordeAIHordeUpdateState.DEAD : EHordeAIHordeUpdateState.ALIVE;
        }

        private void OnHordeEntityKilledEvent(HordeAIEntity entity)
        {
            this.OnHordeEntityKilled?.Invoke(this, new HordeEntityKilledEvent(entity, this));
        }

        private void OnHordeEntityDespawnedEvent(HordeAIEntity entity)
        {
            this.OnHordeEntityDespawned?.Invoke(this, new HordeEntityDespawnedEvent(entity, this));
        }
    }

    public enum EHordeAIHordeUpdateState
    {
        DEAD,
        ALIVE
    }

    public enum EHordeAIStats
    {
        TOTAL_SPAWNED,
        TOTAL_DESPAWNED,
        TOTAL_KILLED,
        TOTAL_ALIVE
    }
}