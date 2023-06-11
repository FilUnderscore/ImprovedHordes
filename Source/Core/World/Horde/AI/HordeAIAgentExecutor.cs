﻿using ImprovedHordes.Core.AI;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.AI
{
    public sealed class HordeAIAgentExecutor : AIAgentExecutor<WorldHorde>
    {
        private readonly IAICommandGenerator<AICommand> commandGenerator;
        private readonly ConcurrentStack<AICommand> interruptCommands = new ConcurrentStack<AICommand>();

        private readonly List<HordeEntityAIAgentExecutor> entities = new List<HordeEntityAIAgentExecutor>();

        public HordeAIAgentExecutor(WorldHorde horde, IAICommandGenerator<AICommand> commandGenerator) : base(horde)
        {
            this.commandGenerator = commandGenerator;
        }

        public void RegisterEntity(HordeEntityAIAgentExecutor entityAIAgentExecutor)
        {
            this.entities.Add(entityAIAgentExecutor);
            entityAIAgentExecutor.SetCommand(this.GetCommand());
        }

        public void UnregisterEntity(HordeEntityAIAgentExecutor entityAIAgentExecutor)
        {
            this.entities.Remove(entityAIAgentExecutor);
        }

        public void Interrupt(params AICommand[] commands)
        {
            this.interruptCommands.Clear();
            this.interruptCommands.PushRange(commands);

            if(this.commandGenerator != null)
                this.commandGenerator.GenerateNextCommand(out this.command);

            foreach (var entity in this.entities)
            {
                entity.SetCommand(this.GetCommand());
            }
        }

        public override GeneratedAICommand<AICommand> GetCommand()
        {
            if (this.interruptCommands.TryPeek(out AICommand currentInterruptCommand))
                return new GeneratedAICommand<AICommand>(currentInterruptCommand);

            return base.GetCommand();
        }

        public GeneratedAICommand<AICommand> GetNextCommand(GeneratedAICommand<AICommand> currentCommand)
        {
            // Try get next interrupt command first.
            if(this.interruptCommands.TryPop(out _))
            {
                if (this.interruptCommands.TryPeek(out AICommand nextInterruptCommand))
                    return new GeneratedAICommand<AICommand>(nextInterruptCommand);
            }

            // Get next generated command.
            if (this.command == currentCommand && this.commandGenerator != null)
                this.commandGenerator.GenerateNextCommand(out this.command);

            return this.command;
        }

        private bool UpdateInterrupts(float dt)
        {
            if (!this.interruptCommands.TryPeek(out AICommand currentInterruptCommand))
                return false;

            if (!currentInterruptCommand.CanExecute(this.agent))
                return false;

            currentInterruptCommand.Execute(this.agent, dt);

            if (!currentInterruptCommand.IsComplete(this.agent))
                return true;

#if DEBUG
            Log.Out($"Horde completed interrupt command {currentInterruptCommand.GetType().Name}");
#endif

            do
            {
                // Discard current command and any expired commands.
                interruptCommands.TryPop(out _);
            }
            while (interruptCommands.TryPeek(out AICommand nextNextCommand) && nextNextCommand.HasExpired());

            return false;
        }

        public override void Update(float dt)
        {
            if (UpdateInterrupts(dt))
                return;

            if (this.UpdateCommand(dt) || this.commandGenerator == null)
                return;

            this.commandGenerator.GenerateNextCommand(out this.command);
        }
        
        /// <summary>
        /// Calculate this agent's objective score. A lower objective score means more important.
        /// </summary>
        /// <returns></returns>
        public int CalculateObjectiveScore()
        {
            int commandScore = 0;

            if (command != null && command.Command != null)
                commandScore = command.Command.GetObjectiveScore(this.agent);

            int interruptScore = 0, interruptCount = 0;
            foreach (var interruptCommand in interruptCommands.ToArray())
            {
                interruptScore += interruptCommand.GetObjectiveScore(this.agent);
                interruptCount++;
            }

            if (interruptCount > 0)
                interruptScore /= interruptCount;

            int score = commandScore - interruptScore;

            return score;
        }
    }
}
