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
                this.executors.Add(entity, new AIAgentExecutor(entity));
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

        private class EntityAIUpdateRequest : IMainThreadRequest
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
                return !this.executor.loaded || this.executor.agent.IsDead();
            }

            public void TickExecute()
            {
                this.executor.Update(Time.deltaTime);

                if(this.executor.agent.IsDead())
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

        public void Queue(AICommand command, bool interrupt = false)
        {
            foreach (var executor in this.executors.Values)
            {
                executor.Queue(command, interrupt);
            }
        }

        private class AIAgentExecutor
        {
            public readonly IAIAgent agent;
            private readonly ConcurrentQueue<AICommand> commands;
            public bool loaded;

            public AIAgentExecutor(IAIAgent agent)
            {
                this.agent = agent;
                this.commands = new ConcurrentQueue<AICommand>();
            }

            public void Update(float dt) 
            {
                if (commands.Count == 0 || !commands.TryPeek(out AICommand nextCommand))
                    return;

                if(!nextCommand.CanExecute(this.agent))
                    return;
                
                nextCommand.Execute(this.agent, dt);

                if (!nextCommand.IsComplete(this.agent))
                    return;

                Log.Out($"Completed command {nextCommand.GetType().Name}");
                commands.TryDequeue(out _);

                while(commands.TryPeek(out AICommand nextNextCommand))
                {
                    if (nextNextCommand.HasExpired())
                        commands.TryDequeue(out _);
                }
            }

            public void Queue(AICommand command, bool interrupt)
            {
                if (command == null)
                    throw new NullReferenceException("Cannot queue a null AICommand.");

                if (interrupt && agent.CanInterrupt())
                {
                    while (commands.TryDequeue(out _)) { }
                }

                commands.Enqueue(command);
            }
        }
    }
}