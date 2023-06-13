using ImprovedHordes.Core.AI;
using ImprovedHordes.POI;

namespace ImprovedHordes.Source.Wandering.Enemy
{
    public sealed class WanderingEnemyAIState : IAIState
    {
        private WorldPOIScanner.Zone targetZone;
        private WanderingState wanderingState;
        private float remainingWanderTime;

        public WanderingEnemyAIState()
        {
            this.targetZone = null;
            this.wanderingState = WanderingState.IDLE;
        }

        public enum WanderingState
        {
            WANDER,
            MOVING,
            IDLE
        }

        public void SetTargetZone(WorldPOIScanner.Zone targetZone)
        {
            this.targetZone = targetZone;
        }

        public void SetWanderingState(WanderingState wanderingState) 
        {
            this.wanderingState = wanderingState;
        }

        public WorldPOIScanner.Zone GetTargetZone()
        {
            return this.targetZone;
        }

        public WanderingState GetWanderingState() 
        {
            return this.wanderingState;
        }

        public float GetRemainingWanderTime()
        {
            return this.remainingWanderTime;
        }

        public void SetRemainingWanderTime(float remainingWanderTime)
        {
            this.remainingWanderTime = remainingWanderTime;
        }
    }
}
