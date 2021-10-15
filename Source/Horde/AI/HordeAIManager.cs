using System;
using System.Collections.Generic;
using UnityEngine;

using static ImprovedHordes.IHLog;

namespace ImprovedHordes.Horde.AI
{
    public sealed class HordeAIManager
    {
        public event EventHandler<HordeKilledEventArgs> OnHordeKilledEvent;

        private readonly Dictionary<Horde, Dictionary<AIHordeEntity, int>> trackedHordes = new Dictionary<Horde, Dictionary<AIHordeEntity, int>>();

        public void Add(EntityAlive entity, Horde horde, List<HordeAICommand> commands)
        {
            if (!trackedHordes.ContainsKey(horde))
            {
                trackedHordes.Add(horde, new Dictionary<AIHordeEntity, int>());
            }

            Dictionary<AIHordeEntity, int> trackedEntities = trackedHordes[horde];

            AIHordeEntity hordeEntity = new AIHordeEntity(entity, commands);
            trackedEntities.Add(hordeEntity, 0);
        }

        public void Add(EntityAlive entity, Horde horde, params HordeAICommand[] commands)
        {
            this.Add(entity, horde, new List<HordeAICommand>(commands));
        }

        public int GetAliveInHorde(Horde horde)
        {
            return trackedHordes[horde].Count;
        }

        public int GetHordesAlive()
        {
            return trackedHordes.Count;
        }

        public void Clear()
        {
            foreach(var entry in trackedHordes)
            {
                Dictionary<AIHordeEntity, int> trackedEntities = entry.Value;

                foreach (var hordeEntity in trackedEntities.Keys)
                {
                    var entity = hordeEntity.alive;

                    if (entity is EntityEnemy enemy)
                        enemy.IsHordeZombie = false;

                    entity.bIsChunkObserver = false;
                }
            }

            trackedHordes.Clear();
        }

        public void Update()
        {
            double dt = Time.fixedDeltaTime;
            List<Horde> hordesToRemove = new List<Horde>();

            foreach (var hordeEntry in trackedHordes)
            {
                Horde horde = hordeEntry.Key;

                Dictionary<AIHordeEntity, int> updates = new Dictionary<AIHordeEntity, int>();
                List<AIHordeEntity> toRemove = new List<AIHordeEntity>();

                foreach (var entityEntry in hordeEntry.Value)
                {
                    AIHordeEntity hordeEntity = entityEntry.Key;
                    EntityAlive entity = hordeEntity.alive;

                    if (entity.IsDead())
                    {
                        toRemove.Add(hordeEntity);
                        continue;
                    }

                    List<HordeAICommand> commands = hordeEntity.commands;

                    int commandIndex = entityEntry.Value;

                    if (commands.Count < commandIndex)
                    {
                        toRemove.Add(hordeEntity);
#if DEBUG
                        Log("Entity {0} has finished executing all commands. Being removed from Horde AI control.", entity.entityId);
#endif
                        continue;
                    }

                    HordeAICommand command = commands[commandIndex];

                    if (command == null)
                    {
                        updates.Add(hordeEntity, commandIndex + 1);
#if DEBUG
                        Warning("Command at index {0} was null for entity {1}. Skipping.", commandIndex, entity.entityId);
#endif
                        continue;
                    }

                    if (command.CanExecute(entity))
                    {
                        Log("Executing");
                        command.Execute(dt, entity);
                    }
                    
                    if(command.IsFinished(entity))
                    {
                        Log("Finished");
                        updates.Add(hordeEntity, commandIndex + 1);
                    }
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    AIHordeEntity hordeEntity = toRemove[i];
                    EntityAlive entity = hordeEntity.alive;

                    if (entity is EntityEnemy enemy)
                        enemy.IsHordeZombie = false;

                    entity.bIsChunkObserver = false;

                    trackedHordes[horde].Remove(hordeEntity);

                    toRemove.RemoveAt(i);
                }

                if (hordeEntry.Value.Count == 0)
                {
                    hordesToRemove.Add(horde);
                    OnHordeKilled(horde);

                    continue;
                }
            }
        }

        private void OnHordeKilled(Horde horde)
        {
            this.OnHordeKilledEvent?.Invoke(this, new HordeKilledEventArgs(horde));
        }

        struct AIHordeEntity
        {
            public EntityAlive alive;
            public List<HordeAICommand> commands;

            public AIHordeEntity(EntityAlive alive, List<HordeAICommand> commands)
            {
                this.alive = alive;
                this.commands = commands;
            }
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
