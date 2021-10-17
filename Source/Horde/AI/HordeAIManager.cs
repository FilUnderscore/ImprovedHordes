using System;
using System.Collections.Generic;
using UnityEngine;

using System.Reflection;
using HarmonyLib;

using static ImprovedHordes.Utils.Logger;

using ImprovedHordes.Horde.AI.Events;

namespace ImprovedHordes.Horde.AI
{
    public sealed class HordeAIManager
    {
        private static readonly MethodInfo DespawnMethod = AccessTools.Method(typeof(EntityAlive), "Despawn");

        static HordeAIManager()
        {
            if (DespawnMethod == null)
                throw new NullReferenceException($"{nameof(DespawnMethod)} is null.");
        }

        public event EventHandler<HordeKilledEvent> OnHordeKilled;

        private readonly Dictionary<Horde, List<HordeAIEntity>> trackedHordes = new Dictionary<Horde, List<HordeAIEntity>>();

        public HordeAIEntity Add(EntityAlive entity, Horde horde, bool despawnOnCompletion, List<HordeAICommand> commands)
        {
            if (!trackedHordes.ContainsKey(horde))
            {
                trackedHordes.Add(horde, new List<HordeAIEntity>());
            }

            List<HordeAIEntity> trackedEntities = trackedHordes[horde];

            HordeAIEntity hordeEntity = new HordeAIEntity(entity, despawnOnCompletion, commands);
            trackedEntities.Add(hordeEntity);

            return hordeEntity;
        }

        public HordeAIEntity Add(EntityAlive entity, Horde horde, bool despawnOnCompletion, params HordeAICommand[] commands)
        {
            return this.Add(entity, horde, despawnOnCompletion, new List<HordeAICommand>(commands));
        }

        public int GetAliveInHorde(Horde horde)
        {
            return trackedHordes[horde].Count;
        }

        public int GetHordesAlive()
        {
            return trackedHordes.Count;
        }

        public void DisbandHorde(Horde horde)
        {
            if (!trackedHordes.ContainsKey(horde))
                return;

            foreach(var entity in trackedHordes[horde])
            {
                UpdateHordeEntity(horde, entity, EHordeEntityUpdateState.FINISHED);
            }
        }

        private readonly Dictionary<Horde, Dictionary<HordeAIEntity, EHordeEntityUpdateState>> updates = new Dictionary<Horde, Dictionary<HordeAIEntity, EHordeEntityUpdateState>>();
        private readonly List<Horde> hordesToRemove = new List<Horde>();

        private void UpdateHordeEntity(Horde horde, HordeAIEntity entity, EHordeEntityUpdateState newState)
        {
            if (!updates.ContainsKey(horde))
                updates.Add(horde, new Dictionary<HordeAIEntity, EHordeEntityUpdateState>());

            var toUpdateDict = updates[horde];

            if (toUpdateDict.ContainsKey(entity))
                toUpdateDict[entity] = newState;
            else
                toUpdateDict.Add(entity, newState);
        }

        public void Update()
        {
            float dt = Time.fixedDeltaTime;
            
            foreach (var hordeEntry in trackedHordes)
            {
                Horde horde = hordeEntry.Key;
                var entities = hordeEntry.Value;

                foreach (var hordeEntity in entities)
                {
                    EntityAlive entity = hordeEntity.alive;

                    if (entity.IsDead())
                    {
                        UpdateHordeEntity(horde, hordeEntity, EHordeEntityUpdateState.DEAD);
                        continue;
                    }

                    // Already awaiting update, so wait until it is processed.
                    if (updates.ContainsKey(horde) && updates[horde].ContainsKey(hordeEntity))
                        continue;

                    List<HordeAICommand> commands = hordeEntity.commands;

                    int commandIndex = hordeEntity.currentCommandIndex;

                    if (commandIndex >= commands.Count)
                    {
                        UpdateHordeEntity(horde, hordeEntity, EHordeEntityUpdateState.FINISHED);
#if DEBUG
                        Log("Entity {0} has finished executing all commands. Being removed from Horde AI control.", entity.entityId);
#endif
                        continue;
                    }

                    HordeAICommand command = commands[commandIndex];

                    if (command == null)
                    {
                        UpdateHordeEntity(horde, hordeEntity, EHordeEntityUpdateState.NEXT);
#if DEBUG
                        Warning("Command at index {0} was null for entity {1}. Skipping.", commandIndex, entity.entityId);
#endif
                        continue;
                    }

                    if (command.CanExecute(entity))
                    {
                        command.Execute(dt, entity);
                    }
                    
                    if(command.IsFinished(entity))
                    {
                        Log("Finished command {0}", command.GetType().FullName);
                        UpdateHordeEntity(horde, hordeEntity, EHordeEntityUpdateState.NEXT);
                    }
                }

                if (entities.Count == 0)
                {
                    hordesToRemove.Add(horde);
                    OnHordeKilledEvent(horde);

                    continue;
                }
            }

            foreach(var updateEntry in updates)
            {
                Horde horde = updateEntry.Key;

                foreach(var entityEntry in updates[horde])
                {
                    HordeAIEntity hordeEntity = entityEntry.Key;
                    EntityAlive entity = hordeEntity.alive;
                    EHordeEntityUpdateState updateState = entityEntry.Value;

                    switch(updateState)
                    {
                        case EHordeEntityUpdateState.NEXT:
                            hordeEntity.currentCommandIndex++;
                            break;
                        case EHordeEntityUpdateState.FINISHED:
                        case EHordeEntityUpdateState.DEAD:
                            if(updateState == EHordeEntityUpdateState.FINISHED)
                            {
                                if (!hordeEntity.despawnOnCompletion)
                                {
                                    if (entity is EntityEnemy enemy)
                                        enemy.IsHordeZombie = false;

                                    entity.bIsChunkObserver = false;
                                }
                                else
                                {
                                    DespawnMethod.Invoke(entity, new object[0]);
                                }
                            }

                            trackedHordes[horde].Remove(hordeEntity);
                            break;
                    }
                }
            }

            foreach(var horde in hordesToRemove)
            {
                trackedHordes.Remove(horde);
            }

            updates.Clear();
            hordesToRemove.Clear();
        }

        private void OnHordeKilledEvent(Horde horde)
        {
            this.OnHordeKilled?.Invoke(this, new HordeKilledEvent(horde));
        }

        private enum EHordeEntityUpdateState
        {
            NEXT,
            FINISHED,
            DEAD
        }
    }
}
