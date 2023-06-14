using ImprovedHordes.Core.AI;
using ImprovedHordes.POI;
using System;
using UnityEngine;

namespace ImprovedHordes.Screamer.Commands
{
    public sealed class ZoneWanderAICommand : AICommand
    {
        private readonly WorldPOIScanner.POIZone zone;
        private readonly Vector2 targetPos;

        public ZoneWanderAICommand(WorldPOIScanner.POIZone zone)
        {
            this.zone = zone;
            this.targetPos = this.zone.GetCenter() + this.zone.GetBounds().size.magnitude * GameManager.Instance.World.GetGameRandom().RandomInsideUnitCircle;
        }

        public override bool CanExecute(IAIAgent agent)
        {
            return agent.GetTarget() == null || !(agent.GetTarget() is EntityPlayer);
        }

        public override void Execute(IAIAgent agent, float dt)
        {

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
