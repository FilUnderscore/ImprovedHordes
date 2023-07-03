using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.AI
{
    public sealed class HordeAIExecutor
    {
        private readonly WorldHorde horde;

        private readonly HordeAIAgentExecutor hordeExecutor;
        private readonly Dictionary<IAIAgent, HordeEntityAIAgentExecutor> executors;

        public HordeAIExecutor(WorldHorde horde, IWorldRandom worldRandom, IAICommandGenerator<AICommand> commandGenerator)
        {
            this.horde = horde;
            
            this.hordeExecutor = new HordeAIAgentExecutor(horde, worldRandom, commandGenerator);
            this.executors = new Dictionary<IAIAgent, HordeEntityAIAgentExecutor>();
        }

        public void AddEntity(HordeClusterEntity entity, IWorldRandom worldRandom, IAICommandGenerator<EntityAICommand> entityCommandGenerator, MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            HordeEntityAIAgentExecutor executor;
            this.executors.Add(entity, executor = new HordeEntityAIAgentExecutor(entity, worldRandom, this.hordeExecutor, entityCommandGenerator));

            this.NotifyEntity(executor, true, mainThreadRequestProcessor);
        }

        public void NotifyEntities(bool loaded, MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            foreach (var executor in this.executors.Values)
            {
                NotifyEntity(executor, loaded, mainThreadRequestProcessor);
            }
        }

        public void Update(float dt)
        {
            if(!this.horde.Spawning && !this.horde.Spawned)
            {
                this.hordeExecutor.Update(dt);
            }
        }

        private void NotifyEntity(HordeEntityAIAgentExecutor executor, bool loaded, MainThreadRequestProcessor mainThreadRequestProcessor)
        {
            if (loaded && !executor.IsLoaded())
            {
                executor.SetLoaded(true);
                mainThreadRequestProcessor.Request(new EntityAIUpdateRequest(executor, this));

                this.hordeExecutor.RegisterEntity(executor);
            }
            else if (!loaded && executor.IsLoaded())
            {
                executor.SetLoaded(false);

                this.hordeExecutor.UnregisterEntity(executor);
            }
        }

        public void Interrupt(params AICommand[] commands)
        {
            this.hordeExecutor.Interrupt(commands);
        }

        public int CalculateObjectiveScore()
        {
            return this.hordeExecutor.CalculateObjectiveScore();
        }

        private sealed class EntityAIUpdateRequest : IMainThreadRequest
        {
            private readonly HordeEntityAIAgentExecutor executor;
            private readonly HordeAIExecutor hordeClusterExecutor;

            public EntityAIUpdateRequest(HordeEntityAIAgentExecutor executor, HordeAIExecutor hordeClusterExecutor)
            {
                this.executor = executor;
                this.hordeClusterExecutor = hordeClusterExecutor;
            }

            public bool IsDone()
            {
                return this.executor.GetAgent().IsDead() || !this.executor.IsLoaded();
            }

            public void TickExecute(float dt)
            {
                this.executor.Update(dt);
            }

            public void OnCleanup()
            {
                if (this.executor.GetAgent().IsDead())
                {
                    this.hordeClusterExecutor.executors.Remove(this.executor.GetAgent());
                }
            }
        }
    }
}