using ImprovedHordes.Core.AI;

namespace ImprovedHordes.Core.World.Horde.AI.Commands
{
    public sealed class SleepingAICommand : AICommand
    {
        private float sleepTime;

        public SleepingAICommand(float sleepTime)
        {
            this.sleepTime = sleepTime;
        }

        public override bool CanExecute(IAIAgent agent)
        {
            return true;
        }

        public override void Execute(IAIAgent agent, float dt)
        {
            sleepTime -= dt;

            if (agent.IsSleeping())
                return;

            agent.Sleep();
        }

        public override int GetObjectiveScore(IAIAgent agent)
        {
            return (int)this.sleepTime * 10;
        }

        public override bool IsComplete(IAIAgent agent)
        {
            return sleepTime <= 0.0f || agent.GetTarget() != null;
        }

        public override void OnInterrupted(IAIAgent agent)
        {
            if(agent.IsSleeping())
                agent.WakeUp();
        }

        public override void OnCompleted(IAIAgent agent)
        {
            if(agent.IsSleeping())
                agent.WakeUp();
        }
    }
}
