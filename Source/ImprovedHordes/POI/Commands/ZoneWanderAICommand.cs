using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;
using ImprovedHordes.POI;
using UnityEngine;

namespace ImprovedHordes.Screamer.Commands
{
    public sealed class ZoneWanderAICommand : GoToTargetAICommand
    {
        private readonly WorldPOIScanner.POIZone zone;
        private readonly float rangeMultiplier;
        private int? wanderCount;

        public ZoneWanderAICommand(WorldPOIScanner.POIZone zone, float rangeMultiplier, int? wanderCount) : base(GetNextTarget(zone, rangeMultiplier))
        {
            this.zone = zone;
            this.rangeMultiplier = rangeMultiplier;
            this.wanderCount = wanderCount;
        }

        private static Vector3 GetNextTarget(WorldPOIScanner.POIZone zone, float rangeMultiplier)
        {
            Vector2 targetPos2 = zone.GetCenter() + (zone.GetBounds().size.magnitude * rangeMultiplier) * GameManager.Instance.World.GetGameRandom().RandomOnUnitCircle;
            float y = GameManager.Instance.World.GetHeightAt(targetPos2.x, targetPos2.y);

            return new Vector3(targetPos2.x, y, targetPos2.y);
        }

        public override bool IsComplete(IAIAgent agent)
        {
            if(this.wanderCount != null && this.wanderCount.Value <= 0)
                return base.IsComplete(agent);

            if(base.IsComplete(agent))
            {
                if (this.wanderCount != null)
                    this.wanderCount--;
                
                this.UpdateTarget(GetNextTarget(this.zone, this.rangeMultiplier));
            }

            return false;
        }
    }
}
