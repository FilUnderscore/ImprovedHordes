using ImprovedHordes.Core.AI;
using System;

namespace ImprovedHordes.Screamer.Commands
{
    public sealed class ZoneWanderAICommand : AICommand
    {
        public ZoneWanderAICommand()
        {

        }

        public override bool CanExecute(IAIAgent agent)
        {
            return agent.GetTarget() == null || !(agent.GetTarget() is EntityPlayer);
        }

        public override void Execute(IAIAgent agent, float dt)
        {
            throw new NotImplementedException();
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
