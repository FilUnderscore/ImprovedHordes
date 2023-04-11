using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace ImprovedHordes.Source.Horde.AI
{
    public sealed class AIExecutor
    {
        private readonly Dictionary<IAIAgent, AIAgentExecutor> agents = new Dictionary<IAIAgent, AIAgentExecutor>();

        // Shared
        private readonly Queue<IAIAgent> agentsToRegister = new Queue<IAIAgent>();
        private readonly object RegisterAgentsLock = new object();

        private readonly Queue<IAIAgent> agentsToRemove = new Queue<IAIAgent>();
        private readonly object RemoveAgentsLock = new object();

        public void Update(float dt)
        {
            if(Monitor.TryEnter(RegisterAgentsLock)) 
            {
                while(agentsToRegister.Count > 0)
                {
                    IAIAgent agent = agentsToRegister.Dequeue();
                    agents.Add(agent, new AIAgentExecutor(agent));
                }

                Monitor.Exit(RegisterAgentsLock);
            }
            
            foreach(KeyValuePair<IAIAgent, AIAgentExecutor> agentEntry in agents)
            {
                IAIAgent agent = agentEntry.Key;
                AIAgentExecutor agentExecutor = agentEntry.Value;

                if (agent.IsDead())
                {
                    if (Monitor.TryEnter(RemoveAgentsLock))
                    {
                        agentsToRemove.Enqueue(agent);
                        Monitor.Exit(RemoveAgentsLock);
                    }
                }
                else
                {
                    agentExecutor.Update(dt);
                }
            }

            if (Monitor.TryEnter(RemoveAgentsLock))
            {
                while (agentsToRemove.Count > 0)
                {
                    agents.Remove(agentsToRemove.Dequeue());
                }

                Monitor.Exit(RemoveAgentsLock);
            }
        }

        public void RegisterAgent(IAIAgent agent)
        {
            Monitor.Enter(this.RegisterAgentsLock);
            this.agentsToRegister.Enqueue(agent);
            Monitor.Exit(this.RegisterAgentsLock);
        }

        public void UnregisterAgent(IAIAgent agent)
        {
            Monitor.Enter(this.RemoveAgentsLock);
            this.agentsToRemove.Enqueue(agent);
            Monitor.Exit(this.RemoveAgentsLock);
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