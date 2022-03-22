using System;
using System.Collections.Generic;
using System.Reflection;

using static ImprovedHordes.Utils.Logger;

using ImprovedHordes.Horde.AI.Commands;
using ImprovedHordes.Horde.AI.Events;

using UnityEngine;
using HarmonyLib;

namespace ImprovedHordes.Horde.AI
{
    public class HordeAIEntity
    {
        private static readonly FieldInfo IsUnloadedField = AccessTools.Field(typeof(Entity), "isUnloaded");

        static HordeAIEntity()
        {
            if (IsUnloadedField == null)
                throw new NullReferenceException($"{nameof(IsUnloadedField)} is null.");
        }

        public EntityAlive entity;
        public bool despawnOnCompletion;
        
        public List<HordeAICommand> commands;
        public int currentCommandIndex = 0;

        public event EventHandler<EntityKilledEvent> OnEntityKilled;
        public event EventHandler<EntityDespawnedEvent> OnEntityDespawned;

        const int SENSE_DIST = 80;
        const float THRESHOLD = 20f;

        public Dictionary<int, SenseEntry> sensations = new Dictionary<int, SenseEntry>();

        public HordeAIEntity(EntityAlive alive, bool despawnOnCompletion, List<HordeAICommand> commands)
        {
            this.entity = alive;
            this.despawnOnCompletion = despawnOnCompletion;
            this.commands = commands;
        }

        public int GetEntityId()
        {
            return this.entity.entityId;
        }

        public HordeAICommand GetCurrentCommand()
        {
            if (this.currentCommandIndex >= this.commands.Count)
                return null;

            return commands[this.currentCommandIndex];
        }

        public void InterruptWithNewCommands(params HordeAICommand[] commands)
        {
            int size = this.commands.Count;
            this.commands.AddRange(commands);

            this.currentCommandIndex = 0;
            this.commands.RemoveRange(0, size);
        }

        public EHordeAIEntityUpdateState Update(float dt)
        {
            if ((bool)IsUnloadedField.GetValue(this.entity) || this.entity.IsDead())
            {
                this.OnEntityKilledEvent();

                return EHordeAIEntityUpdateState.DEAD;
            }

            if (this.currentCommandIndex >= this.commands.Count)
            {
                if (this.despawnOnCompletion)
                    this.OnEntityDespawnedEvent();

                return EHordeAIEntityUpdateState.FINISHED;
            }

            HordeAICommand command = this.commands[this.currentCommandIndex];

            if (tickSense(out SenseEntry entry))
            {
                if ((command == null || command.GetType() != typeof(HordeAICommandInvestigate)))
                {
                    this.commands.Insert(this.currentCommandIndex, new HordeAICommandInvestigate(entry));
                }
                else
                {
                    HordeAICommandInvestigate hordeAICommandInvestigate = (HordeAICommandInvestigate)command;

                    SenseEntry oldEntry = hordeAICommandInvestigate.GetEntry();

                    if(oldEntry != entry)
                    {
                        if(!oldEntry.IsSeen() && entry.IsSeen())
                        {
                            hordeAICommandInvestigate.UpdateEntry(entry);
                        }
                        else if(oldEntry.IsSeen() && entry.IsSeen() && (oldEntry.position - entity.position).sqrMagnitude < (entry.position - entity.position).sqrMagnitude)
                        {
                            hordeAICommandInvestigate.UpdateEntry(entry);
                        }
                    }

                    return EHordeAIEntityUpdateState.CONTINUE_COMMAND;
                }
            }

            if (command == null)
            {
                Warning("Command at index {0} was null for entity {1}. Skipping.", this.currentCommandIndex, entity.entityId);

                return EHordeAIEntityUpdateState.NEXT_COMMAND;
            }

            if(command.CanExecute(this.entity))
            {
                command.Execute(dt, this.entity);
            }

            if(command.IsFinished(this.entity))
            {
#if DEBUG
                Log("Finished command {0}", command.GetType().FullName);
#endif

                return EHordeAIEntityUpdateState.NEXT_COMMAND;
            }

            return EHordeAIEntityUpdateState.CONTINUE_COMMAND;
        }

        private bool tickSense(out SenseEntry entry)
        {
            if (!(this.entity is EntityEnemy))
            {
                entry = null;
                return false;
            }

            for(int i = 0; i < this.entity.world.Players.Count; i++)
            {
                EntityPlayer player = this.entity.world.Players.list[i];
                
                if((player.position - this.entity.position).sqrMagnitude <= (SENSE_DIST * SENSE_DIST))
                {
                    if (!sensations.ContainsKey(player.entityId))
                    {
                        sensations.Add(player.entityId, new SenseEntry
                        {
                            player = player,
                            entity = this.entity
                        });
                    }

                    entry = sensations[player.entityId];
                    entry.Update();

                    Log.Out($"Player {entry.player.EntityName} {entry.position} S {entry.GetSound()} L {entry.GetLight()} C {entry.GetValue()} D {(entry.player.position - entity.position).magnitude}");

                    if (entry.GetValue() > THRESHOLD)
                        return true;
                }
            }

            entry = null;
            return false;
        }

        public class SenseEntry
        {
            public Vector3 position;
            public EntityAlive entity;
            public EntityPlayer player;
            public PlayerStealth stealth;

            public float GetValue()
            {
                float distancePct = Mathf.Clamp01(1f - (entity.position - player.position).sqrMagnitude / (SENSE_DIST * SENSE_DIST));

                return (GetSound() + GetLight() * 0.5f) * distancePct;
            }

            public float GetSound()
            {
                return stealth.noiseVolume;
            }

            public float GetLight()
            {
                return IsSeen() ? stealth.lightLevel : 0.0f;
            }

            public bool IsSeen()
            {
                return entity.CanSee(this.player);
            }

            public void Update()
            {
                if(GetValue() > THRESHOLD)
                    this.position = player.position;

                this.stealth = player.Stealth;
            }
        }

        private void OnEntityKilledEvent()
        {
            this.OnEntityKilled?.Invoke(this, new EntityKilledEvent(this));
        }

        private void OnEntityDespawnedEvent()
        {
            this.OnEntityDespawned?.Invoke(this, new EntityDespawnedEvent(this));
        }
    }

    public enum EHordeAIEntityUpdateState
    {
        DEAD,
        FINISHED,
        NEXT_COMMAND,
        CONTINUE_COMMAND
    }
}
