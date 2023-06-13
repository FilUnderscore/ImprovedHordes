using ImprovedHordes.Core.AI;

namespace ImprovedHordes.Core.World.Horde.AI.Commands
{
    public sealed class WanderAICommand : AICommand
    {
        private float wanderTime;

        public WanderAICommand(float wanderTime) 
        {
            this.wanderTime = wanderTime;
        }

        public override bool CanExecute(IAIAgent agent)
        {
            return true;
        }

        public override void Execute(IAIAgent agent, float dt)
        {
            this.wanderTime -= dt;
        }

        public override int GetObjectiveScore(IAIAgent agent)
        {
            return (int)(this.wanderTime * 10);
        }

        public override bool IsComplete(IAIAgent agent)
        {
            return this.wanderTime <= 0.0f;
        }

        public float GetWanderTime()
        {
            return this.wanderTime;
        }
    }
}
