using ImprovedHordes.Core.AI;
using ImprovedHordes.POI;
using ImprovedHordes.POI.Prefab;

namespace ImprovedHordes.Screamer
{
    public sealed class ScreamerAIState : IAIState
    {
        public enum WanderState
        {
            MOVING,
            IDLE
        }

        private WanderState wanderState;
        private readonly PrefabPOIZone zone;
        private float remainingWanderTime;

        public ScreamerAIState(PrefabPOIZone zone)
        {
            this.zone = zone;
            this.wanderState = WanderState.IDLE;
        }

        public WanderState GetWanderState()
        {
            return this.wanderState;
        }

        public PrefabPOIZone GetPOIZone()
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
