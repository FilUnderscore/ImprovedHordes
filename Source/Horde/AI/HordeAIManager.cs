using System;
using System.Collections.Generic;
using UnityEngine;

using System.Reflection;
using HarmonyLib;

using static ImprovedHordes.Utils.Logger;

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

        public event EventHandler<HordeKilledEventArgs> OnHordeKilledEvent;

        private readonly Dictionary<Horde, Dictionary<AIHordeEntity, int>> trackedHordes = new Dictionary<Horde, Dictionary<AIHordeEntity, int>>();

        public void Add(EntityAlive entity, Horde horde, bool despawnOnCompletion, List<HordeAICommand> commands)
        {
            if (!trackedHordes.ContainsKey(horde))
            {
                trackedHordes.Add(horde, new Dictionary<AIHordeEntity, int>());
            }

            Dictionary<AIHordeEntity, int> trackedEntities = trackedHordes[horde];

            AIHordeEntity hordeEntity = new AIHordeEntity(entity, despawnOnCompletion, commands);
            trackedEntities.Add(hordeEntity, 0);
        }

        public void Add(EntityAlive entity, Horde horde, bool despawnOnCompletion, params HordeAICommand[] commands)
        {
            this.Add(entity, horde, despawnOnCompletion, new List<HordeAICommand>(commands));
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

            foreach(var entityEntry in trackedHordes[horde])
            {
                AIHordeEntity entity = entityEntry.Key;

                UpdateHordeEntity(horde, entity, EHordeEntityUpdateState.FINISHED);
            }
        }

        private void Clear()
        {
            foreach(var entry in trackedHordes)
            {
                Dictionary<AIHordeEntity, int> trackedEntities = entry.Value;

                foreach (var hordeEntity in trackedEntities.Keys)
                {
                    var entity = hordeEntity.alive;

                    if (entity.IsDead())
                        continue;

                    if (entity is EntityEnemy enemy)
                        enemy.IsHordeZombie = false;

                    entity.bIsChunkObserver = false;
                }
            }

            trackedHordes.Clear();
            updates.Clear();
            hordesToRemove.Clear();
        }

        private readonly Dictionary<Horde, Dictionary<AIHordeEntity, EHordeEntityUpdateState>> updates = new Dictionary<Horde, Dictionary<AIHordeEntity, EHordeEntityUpdateState>>();
        private readonly List<Horde> hordesToRemove = new List<Horde>();

        private void UpdateHordeEntity(Horde horde, AIHordeEntity entity, EHordeEntityUpdateState newState)
        {
            if (!updates.ContainsKey(horde))
                updates.Add(horde, new Dictionary<AIHordeEntity, EHordeEntityUpdateState>());

            var toUpdateDict = updates[horde];

            if (toUpdateDict.ContainsKey(entity))
                toUpdateDict[entity] = newState;
            else
                toUpdateDict.Add(entity, newState);
        }

        public void Update()
        {
            double dt = Time.fixedDeltaTime;
            
            foreach (var hordeEntry in trackedHordes)
            {
                Horde horde = hordeEntry.Key;
                var entities = hordeEntry.Value;

                foreach (var entityEntry in entities)
                {
                    AIHordeEntity hordeEntity = entityEntry.Key;
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

                    int commandIndex = entityEntry.Value;

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
                    OnHordeKilled(horde);

                    continue;
                }
            }

            foreach(var updateEntry in updates)
            {
                Horde horde = updateEntry.Key;

                foreach(var entityEntry in updates[horde])
                {
                    AIHordeEntity hordeEntity = entityEntry.Key;
                    EntityAlive entity = hordeEntity.alive;
                    EHordeEntityUpdateState updateState = entityEntry.Value;

                    switch(updateState)
                    {
                        case EHordeEntityUpdateState.NEXT:
                            trackedHordes[horde][hordeEntity]++;
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

        private void OnHordeKilled(Horde horde)
        {
            this.OnHordeKilledEvent?.Invoke(this, new HordeKilledEventArgs(horde));
        }

        struct AIHordeEntity
        {
            public EntityAlive alive;
            public bool despawnOnCompletion;
            public List<HordeAICommand> commands;
            
            public AIHordeEntity(EntityAlive alive, bool despawnOnCompletion, List<HordeAICommand> commands)
            {
                this.alive = alive;
                this.despawnOnCompletion = despawnOnCompletion;
                this.commands = commands;
            }
        }

        enum EHordeEntityUpdateState
        {
            NEXT,
            FINISHED,
            DEAD
        }

        public class HordeKilledEventArgs : EventArgs
        {
            public readonly Horde horde;

            public HordeKilledEventArgs(Horde horde)
            {
                this.horde = horde;
            }
        }
    }
}
