using System;
using System.Collections.Generic;

using static ImprovedHordes.Utils.Logger;

using ImprovedHordes.Horde.AI.Events;

namespace ImprovedHordes.Horde.AI
{
    public class HordeAIEntity
    {
        public EntityAlive entity;
        public bool despawnOnCompletion;
        
        public List<HordeAICommand> commands;
        public int currentCommandIndex = 0;

        public event EventHandler<HordeEntityKilledEvent> OnHordeEntityKilled;
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
            if (this.entity.IsDead())
            {
                this.OnHordeEntityKilledEvent();
                return EHordeAIEntityUpdateState.DEAD;
            }

            if (this.currentCommandIndex >= this.commands.Count)
                return EHordeAIEntityUpdateState.FINISHED;

            HordeAICommand command = this.commands[this.currentCommandIndex];

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
                Log("Finished command {0}", command.GetType().FullName);

                return EHordeAIEntityUpdateState.NEXT_COMMAND;
            }

            return EHordeAIEntityUpdateState.CONTINUE_COMMAND;
        }

        private void OnHordeEntityKilledEvent()
        {
            this.OnHordeEntityKilled?.Invoke(this, new HordeEntityKilledEvent(this));
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
