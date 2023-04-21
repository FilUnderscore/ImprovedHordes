using ImprovedHordes.Source.Core.Horde.World.Cluster;
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
        private readonly Dictionary<IAIAgent, AIAgentExecutor> executors;

        private readonly AIAgentExecutor hordeExecutor;
        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;

        public HordeAIExecutor(WorldHorde horde)
        {
            this.horde = horde;

            this.executors = new Dictionary<IAIAgent, AIAgentExecutor>();
            this.hordeExecutor = new AIAgentExecutor(horde);

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
            AIAgentExecutor executor;
            this.executors.Add(entity, executor = new AIAgentExecutor(entity));

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

            public void TickExecute()
            {
                this.executor.Update(Time.deltaTime);
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

        public void Queue(bool interrupt = false, params AICommand[] commands)
        {
            this.hordeExecutor.Queue(interrupt, commands);

            foreach (var executor in this.executors.Values)
            {
                executor.Queue(interrupt, commands);
            }
        }

        private sealed class AIAgentExecutor
        {
            public readonly IAIAgent agent;
            private readonly ConcurrentQueue<AICommand> commands;
            private readonly ConcurrentStack<AICommand> interruptCommands;
            public bool loaded;

            public AIAgentExecutor(IAIAgent agent)
            {
                this.agent = agent;
                this.commands = new ConcurrentQueue<AICommand>();
                this.interruptCommands = new ConcurrentStack<AICommand>();
            }

            public void CopyTo(AIAgentExecutor executor)
            {
                foreach (var command in commands.ToArray())
                    executor.commands.Enqueue(command);

                foreach(var interruptCommand in interruptCommands.ToArray())
                    executor.interruptCommands.Push(interruptCommand);
            }

            public void Update(float dt) 
            {
                if (commands.Count == 0 && interruptCommands.Count == 0)
                    return;

                if(agent.CanInterrupt())
                {
                    this.UpdateInterruptCommands(dt);
                    return;
                }

                this.UpdateCommands(dt);
            }

            private void UpdateInterruptCommands(float dt)
            {
                if (!interruptCommands.TryPeek(out AICommand nextCommand))
                    return;

                if (!nextCommand.CanExecute(this.agent))
                    return;

                nextCommand.Execute(this.agent, dt);

                if (!nextCommand.IsComplete(this.agent))
                    return;
#if DEBUG
                Log.Out($"Completed interrupt command {nextCommand.GetType().Name}");
#endif
                interruptCommands.TryPop(out _);

                while (interruptCommands.TryPeek(out AICommand nextNextCommand))
                {
                    if (nextNextCommand.HasExpired())
                        interruptCommands.TryPop(out _);
                }
            }

            private void UpdateCommands(float dt)
            {
                if (!commands.TryPeek(out AICommand nextCommand))
                    return;

                if (!nextCommand.CanExecute(this.agent))
                    return;

                nextCommand.Execute(this.agent, dt);

                if (!nextCommand.IsComplete(this.agent))
                    return;

#if DEBUG
                Log.Out($"Completed command {nextCommand.GetType().Name}");
#endif
                commands.TryDequeue(out _);

                while (commands.TryPeek(out AICommand nextNextCommand))
                {
                    if (nextNextCommand.HasExpired())
                        commands.TryDequeue(out _);
                }
            }

            public void Queue(bool interrupt, params AICommand[] commands)
            {
                if (commands == null)
                    throw new NullReferenceException("Cannot queue a null AICommand.");

                if (interrupt)
                {
                    while (interruptCommands.TryPop(out _)) { }
                    interruptCommands.PushRange(commands);
                }
                else
                {
                    foreach(var command in commands)
                        this.commands.Enqueue(command);
                }
            }

            /// <summary>
            /// Calculate this agent's objective score. A lower objective score means more important.
            /// </summary>
            /// <returns></returns>
            public int CalculateObjectiveScore()
            {
                int commandScore = 0, commandCount = 0;
                foreach(var command in commands.ToArray())
                {
                    commandScore += command.GetObjectiveScore(this.agent);
                    commandCount++;
                }

                if(commandCount > 0)
                    commandScore /= commandCount;

                int interruptScore = 0, interruptCount = 0;
                foreach(var interruptCommand in interruptCommands.ToArray())
                {
                    interruptScore += interruptCommand.GetObjectiveScore(this.agent);
                    interruptCount++;
                }

                if(interruptCount > 0)
                    interruptScore /= interruptCount;

                int score = commandScore - interruptScore;
                
                return score;
            }
        }
    }
}