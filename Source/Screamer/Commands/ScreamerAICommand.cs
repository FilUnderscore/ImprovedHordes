using ImprovedHordes.Core.AI;
using System;

namespace ImprovedHordes.Screamer.Commands
{
    public sealed class ScreamerAICommand : AICommand
    {
        private const float SCREAM_DELAY = 18.0f;

        private float screamTicks = SCREAM_DELAY;
        private int screamCount = 0;

        public override bool CanExecute(IAIAgent agent)
        {
            return true;
        }

        public override void Execute(IAIAgent agent, float dt)
        {
            if (agent.GetTarget() == null || !(agent.GetTarget() is EntityPlayer))
                return;

            if(screamTicks <= 0.0)
            {

            }
        }

        public override int GetObjectiveScore(IAIAgent agent)
        {
            throw new NotImplementedException();
        }

        public override bool IsComplete(IAIAgent agent)
        {
            throw new NotImplementedException();
        }
    }
}
