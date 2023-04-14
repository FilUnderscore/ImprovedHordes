using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Source.Horde.AI
{
    public sealed class HordeClusterAIExecutor
    {
        private readonly HordeCluster cluster;
        private readonly Dictionary<IAIAgent, AIAgentExecutor> executors;

        private readonly AIAgentExecutor clusterExecutor;
        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;

        public HordeClusterAIExecutor(HordeCluster cluster)
        {
            this.cluster = cluster;

            this.executors = new Dictionary<IAIAgent, AIAgentExecutor>();
            this.clusterExecutor = new AIAgentExecutor(cluster);

            if(ImprovedHordesCore.TryGetInstance(out var instance))
            {
                this.mainThreadRequestProcessor = instance.GetMainThreadRequestProcessor();
            }
        }

        public void AddEntities(IEnumerable<HordeClusterEntity> entities) 
        {
            foreach(var entity in entities)
            {
                AIAgentExecutor executor;
                this.executors.Add(entity, executor = new AIAgentExecutor(entity));

                this.clusterExecutor.CopyTo(executor);
            }
        }

        public void Notify(bool loaded)
        {
            NotifyEntities(loaded);
        }

        public void Update(float dt)
        {
            if(!this.cluster.IsSpawned())
            {
                this.UpdateCluster(dt);
            }
        }

        private void NotifyEntities(bool loaded)
        {
            foreach(var executor in this.executors.Values)
            {
                if(loaded && !executor.loaded)
                {
                    executor.loaded = true;
                    mainThreadRequestProcessor.Request(new EntityAIUpdateRequest(executor, this));
                    Log.Out("Requesting entity updates");
                }
                else if(!loaded && executor.loaded)
                {
                    executor.loaded = false;
                    Log.Out("Stopping entity updates");
                }
            }
        }

        private sealed class EntityAIUpdateRequest : IMainThreadRequest
        {
            private readonly AIAgentExecutor executor;
            private readonly HordeClusterAIExecutor hordeClusterExecutor;

            public EntityAIUpdateRequest(AIAgentExecutor executor, HordeClusterAIExecutor hordeClusterExecutor)
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
                    Log.Out("Dead agent, removing");
                }
            }
        }

        private void UpdateCluster(float dt)
        {
            this.clusterExecutor.Update(dt);
        }

        public void Queue(bool interrupt = false, params AICommand[] commands)
        {
            this.clusterExecutor.Queue(interrupt, commands);

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

                Log.Out($"Completed interrupt command {nextCommand.GetType().Name}");
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

                Log.Out($"Completed command {nextCommand.GetType().Name}");
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
        }
    }
}