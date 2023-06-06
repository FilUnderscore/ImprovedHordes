using ImprovedHordes.Source.Horde.AI;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster.AI
{
    public sealed class HordeEntityAIAgentExecutor : AIAgentExecutor
    {
        private readonly HordeAIAgentExecutor hordeAIAgentExecutor;
        private bool loaded;

        public HordeEntityAIAgentExecutor(IAIAgent agent, HordeAIAgentExecutor hordeAIAgentExecutor) : base(agent)
        {
            this.hordeAIAgentExecutor = hordeAIAgentExecutor;
        }

        public override void Update(float dt)
        {
            if (this.UpdateCommand(dt))
                return;

            this.command = this.hordeAIAgentExecutor.GetNextCommand(this.command);
        }

        public bool IsLoaded()
        {
            return this.loaded;
        }

        public void SetLoaded(bool loaded)
        {
            this.loaded = loaded;
        }
    }
}
