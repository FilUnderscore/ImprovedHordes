using ImprovedHordes.Core.AI;
using ImprovedHordes.POI;

namespace ImprovedHordes.Screamer
{
    public sealed class ScreamerAIState : IAIState
    {
        public enum WanderState
        {
            WANDERING,
            MOVING,
            IDLE
        }

        private WanderState wanderState;
        private readonly WorldPOIScanner.POIZone zone;
        private float remainingWanderTime;

        public ScreamerAIState(WorldPOIScanner.POIZone zone)
        {
            this.zone = zone;
            this.wanderState = WanderState.IDLE;
        }

        public WanderState GetWanderState()
        {
            return this.wanderState;
        }

        public WorldPOIScanner.POIZone GetPOIZone()
        {
            return this.zone;
        }

        public float GetRemainingWanderTime()
        {
            return this.remainingWanderTime;
        }

        public void SetWanderState(WanderState wanderState) 
        {
            this.wanderState = wanderState;
        }

        public void SetRemainingWanderTime(float remainingWanderTime) 
        {
            this.remainingWanderTime = remainingWanderTime;
        }
    }
}
