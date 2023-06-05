using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;
using ImprovedHordes.Source.Core.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Source.Horde.AI
{
    public sealed class HordeAIExecutor
    {
        private readonly WorldHorde horde;
        private readonly Dictionary<IAIAgent, HordeEntityAIAgentExecutor> executors;

        private readonly HordeAIAgentExecutor hordeExecutor;
        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;

        public HordeAIExecutor(WorldHorde horde, IAICommandGenerator commandGenerator)
        {
            this.horde = horde;

            this.executors = new Dictionary<IAIAgent, HordeEntityAIAgentExecutor>();
            this.hordeExecutor = new HordeAIAgentExecutor(horde, this, commandGenerator);
            
            if(ImprovedHordesCore.TryGetInstance(out var instance))
            {
                this.mainThreadRequestProcessor = instance.GetMainThreadRequestProcessor();
            }
        }

        public void AddEntities(IEnumerable<HordeClusterEntity> entities, bool loaded)
        {
            foreach(var entity in entities)
            {
                AddEntity(entity, loaded);
            }
        }

        public void AddEntity(HordeClusterEntity entity, bool loaded)
        {
            HordeEntityAIAgentExecutor executor;
            this.executors.Add(entity, executor = new HordeEntityAIAgentExecutor(entity, this.hordeExecutor));

            this.hordeExecutor.CopyTo(executor);
            this.NotifyEntity(executor, loaded);
        }

        public void Notify(bool loaded)
        {
            NotifyEntities(loaded);
        }

        public void Update(float dt)
        {
            if(!this.horde.IsSpawned())
            {
                this.UpdateCluster(dt);
            }
        }

        private void NotifyEntity(AIAgentExecutor executor, bool loaded)
        {
            if (loaded && !executor.loaded)
            {
                executor.loaded = true;
                mainThreadRequestProcessor.Request(new EntityAIUpdateRequest(executor, this));
            }
            else if (!loaded && executor.loaded)
            {
                executor.loaded = false;
            }
        }

        private void NotifyEntities(bool loaded)
        {
            foreach(var executor in this.executors.Values)
            {
                NotifyEntity(executor, loaded);   
            }
        }

        public int CalculateObjectiveScore()
        {
            return this.hordeExecutor.CalculateObjectiveScore();
        }

        private sealed class EntityAIUpdateRequest : IMainThreadRequest
        {
            private readonly AIAgentExecutor executor;
            private readonly HordeAIExecutor hordeClusterExecutor;

            public EntityAIUpdateRequest(AIAgentExecutor executor, HordeAIExecutor hordeClusterExecutor)
            {
                this.executor = executor;
                this.hordeClusterExecutor = hordeClusterExecutor;
            }

            public bool IsDone()
            {
                return this.executor.agent.IsDead() || !this.executor.loaded;
            }

            public void TickExecute(float dt)
            {
                this.executor.Update(dt);
            }

            public void OnCleanup()
            {
                if (this.executor.agent.IsDead())
                {
                    this.hordeClusterExecutor.executors.Remove(this.executor.agent);
                }
            }
        }

        private void UpdateCluster(float dt)
        {
            this.hordeExecutor.Update(dt);
        }

        public void Interrupt(params AICommand[] commands)
        {
            this.hordeExecutor.Interrupt(commands);
            this.hordeExecutor.GenerateNewCommand();

            foreach (var executor in this.executors.Values)
            {
                executor.Interrupt(commands);
            }
        }

        public void NotifyEntities(AICommand command)
        {
            foreach(var executor in this.executors.Values)
            {
                executor.command = command;
            }
        }

        private abstract class AIAgentExecutor
        {
            public readonly IAIAgent agent;
            public bool loaded;

            public AICommand command;
            public readonly ConcurrentStack<AICommand> interruptCommands;

            public AIAgentExecutor(IAIAgent agent)
            {
                this.agent = agent;
                this.interruptCommands = new ConcurrentStack<AICommand>();
            }

            public void Update(float dt)
            {
                if (interruptCommands.Count > 0)
                {
                    if (agent.CanInterrupt() && this.UpdateInterruptCommands(dt))
                    {
                        return;
                    }
                }

                this.UpdateCommand(dt);
            }

            private bool UpdateInterruptCommands(float dt)
            {
                if (!interruptCommands.TryPeek(out AICommand nextCommand))
                    return false;

                if (!nextCommand.CanExecute(this.agent))
                    return false;

                nextCommand.Execute(this.agent, dt);

                if (!nextCommand.IsComplete(this.agent))
                    return true;
#if DEBUG
                Log.Out($"Completed interrupt command {nextCommand.GetType().Name}");
#endif
                interruptCommands.TryPop(out _);

                while (interruptCommands.TryPeek(out AICommand nextNextCommand))
                {
                    if (nextNextCommand.HasExpired())
                        interruptCommands.TryPop(out _);
                }

                return false;
            }

            protected virtual bool UpdateCommand(float dt)
            {
                if (command == null)
                    return false;

                if (!command.CanExecute(this.agent))
                    return false;

                command.Execute(this.agent, dt);

                if (!command.IsComplete(this.agent))
                    return false;

#if DEBUG
                Log.Out($"Completed command {command.GetType().Name}");
#endif
                return true;
            }

            public void Interrupt(params AICommand[] commands)
            {
                if (commands == null)
                    throw new NullReferenceException("Cannot queue a null AICommand.");

                while (interruptCommands.TryPop(out _)) { }
                interruptCommands.PushRange(commands);
            }

            /// <summary>
            /// Calculate this agent's objective score. A lower objective score means more important.
            /// </summary>
            /// <returns></returns>
            public int CalculateObjectiveScore()
            {
                int commandScore = 0;

                if (command != null)
                    commandScore = command.GetObjectiveScore(this.agent);

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

        private sealed class HordeEntityAIAgentExecutor : AIAgentExecutor
        {
            private readonly HordeAIAgentExecutor hordeAIAgentExecutor;
            private readonly List<AICommand> extraCommands = new List<AICommand>();

            public HordeEntityAIAgentExecutor(IAIAgent agent, HordeAIAgentExecutor hordeAIAgentExecutor, params IAICommandGenerator[] extraCommandGenerators) : base(agent)
            {
                this.hordeAIAgentExecutor = hordeAIAgentExecutor;

                foreach (var commandGenerator in extraCommandGenerators)
                {
                    if (!commandGenerator.GenerateNextCommand(out AICommand command))
                        continue;

                    extraCommands.Add(command);
                }
            }

            protected override bool UpdateCommand(float dt)
            {
                bool result = base.UpdateCommand(dt);

                foreach(var command in extraCommands)
                {
                    if (command.CanExecute(agent))
                        command.Execute(agent, dt);
                }

                if (!result)
                {
                    return false;
                }

                this.hordeAIAgentExecutor.GenerateNewCommand();
                return true;
            }
        }

        private sealed class HordeAIAgentExecutor : AIAgentExecutor
        {
            private readonly HordeAIExecutor executor;
            private readonly IAICommandGenerator commandGenerator;

            public HordeAIAgentExecutor(WorldHorde horde, HordeAIExecutor executor, IAICommandGenerator commandGenerator) : base(horde)
            {
                this.executor = executor;
                this.commandGenerator = commandGenerator;
            }

            public void CopyTo(HordeEntityAIAgentExecutor executor)
            {
                executor.command = this.command;
                
                foreach(var interruptCommand in interruptCommands.ToArray())
                    executor.interruptCommands.Push(interruptCommand);
            }

            protected override bool UpdateCommand(float dt)
            {
                if (command == null || command.IsComplete(this.agent) || command.HasExpired())
                {
                    GenerateNewCommand();
                }

                return base.UpdateCommand(dt);
            }

            // Called when a HordeEntityAIAgentExecutor completes the current command while the HordeAIAgentExecutor is not running.
            public void GenerateNewCommand()
            {
                this.interruptCommands.Clear(); // Reset interrupts otherwise entities will keep coming back after despawning due to HordeAIAgentExecutor not having interrupts cleared, which is then copied onto the entities when spawned.

                if (commandGenerator == null)
                    return;

                if (!this.commandGenerator.GenerateNextCommand(out command))
                    return;

                this.executor.NotifyEntities(command);
            }
        }
    }
}