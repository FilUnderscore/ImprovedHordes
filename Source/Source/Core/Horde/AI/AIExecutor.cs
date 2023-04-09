using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Horde.AI
{
    public sealed class AIExecutor
    {
        private readonly Dictionary<IAIAgent, AIAgentExecutor> agents = new Dictionary<IAIAgent, AIAgentExecutor>();
        private readonly ConcurrentQueue<IAIAgent> agentsToRegister = new ConcurrentQueue<IAIAgent>();
        private readonly Queue<IAIAgent> agentsToRemove = new Queue<IAIAgent>();

        public void Update(float dt)
        {
            while(agentsToRegister.TryDequeue(out IAIAgent agent))
            {
                agents.Add(agent, new AIAgentExecutor(this, agent));
            }

            foreach(KeyValuePair<IAIAgent, AIAgentExecutor> agentEntry in agents)
            {
                IAIAgent agent = agentEntry.Key;
                AIAgentExecutor agentExecutor = agentEntry.Value;

                if (agent.IsDead())
                {
                    agentsToRemove.Enqueue(agent);
                }
                else
                {
                    agentExecutor.Update(dt);
                }
            }

            while(agentsToRemove.Count > 0)
            {
                agents.Remove(agentsToRemove.Dequeue());
            }
        }

        public void RegisterAgent(IAIAgent agent)
        {
            this.agentsToRegister.Enqueue(agent);
        }

        public void Queue(IAIAgent agent, IAICommand command, bool interrupt = false)
        {
            if (!this.agents.TryGetValue(agent, out AIAgentExecutor executor))
                return;

            executor.Queue(command, interrupt);
        }

        private class AIAgentExecutor
        {
            private readonly IAIAgent agent;
            private readonly ConcurrentQueue<IAICommand> commands;

            public AIAgentExecutor(AIExecutor executor, IAIAgent agent)
            {
                this.agent = agent;
                this.commands = new ConcurrentQueue<IAICommand>();
            }

            public void Update(float dt) 
            {
                if (commands.Count == 0 || !commands.TryPeek(out IAICommand nextCommand))
                    return;

                if(!nextCommand.CanExecute(this.agent))
                    return;
                
                nextCommand.Execute(this.agent, dt);

                if (!nextCommand.IsComplete(this.agent))
                    return;

                commands.TryDequeue(out _);
            }

            public void Queue(IAICommand command, bool interrupt)
            {
                if (command == null)
                    throw new NullReferenceException("Cannot queue a null AICommand.");

                if (interrupt)
                {
                    while (commands.TryDequeue(out _)) { }
                }

                commands.Enqueue(command);
            }
        }
    }
}