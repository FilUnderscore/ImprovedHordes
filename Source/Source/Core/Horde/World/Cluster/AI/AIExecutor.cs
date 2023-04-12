using ImprovedHordes.Source.Core.Horde.World.Cluster;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Horde.AI
{
    public sealed class HordeClusterAIExecutor
    {
        private readonly Dictionary<IAIAgent, AIAgentExecutor> agents = new Dictionary<IAIAgent, AIAgentExecutor>();
        private readonly HordeCluster cluster;

        public HordeClusterAIExecutor(HordeCluster cluster)
        {
            this.cluster = cluster;
        }

        public void Update(float dt)
        {

        }

        public void RegisterAgent(IAIAgent agent)
        {
        }

        public void UnregisterAgent(IAIAgent agent)
        {
        }

        public void Queue(IAIAgent agent, AICommand command, bool interrupt = false)
        {
            if (!this.agents.TryGetValue(agent, out AIAgentExecutor executor))
                return;

            executor.Queue(command, interrupt);
        }

        private class AIAgentExecutor
        {
            private readonly IAIAgent agent;
            private readonly ConcurrentQueue<AICommand> commands;

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