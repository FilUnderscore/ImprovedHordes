using System.Collections.Generic;
using System.Reflection;
using System;

using HarmonyLib;

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

        public HordeAIHorde(Horde horde)
        {
            this.horde = horde;
        }

        public Horde GetHordeInstance()
        {
            return this.horde;
        }

        public void AddEntity(HordeAIEntity entity)
        {
            if (entities.ContainsKey(entity.GetEntityId()))
                return;

            entities.Add(entity.GetEntityId(), entity);
        }

        public HordeAIEntity GetEntity(int entityId)
        {
            if (!entities.ContainsKey(entityId))
                return null;

            return entities[entityId];
        }

        public void Disband()
        {
            foreach(var entity in this.entities.Values)
            {
                entity.currentCommandIndex = entity.commands.Count;
            }
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
                        if(entityUpdateState == EHordeAIEntityUpdateState.FINISHED)
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
                            }
                        }

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

            return this.entities.Count == 0 ? EHordeAIHordeUpdateState.DEAD : EHordeAIHordeUpdateState.ALIVE;
        }
    }

    public enum EHordeAIHordeUpdateState
    {
        DEAD,
        ALIVE
    }
}