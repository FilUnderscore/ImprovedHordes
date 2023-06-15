using ImprovedHordes.Core.AI;
using UnityEngine;

namespace ImprovedHordes.Wandering.Animal
{
    public sealed class WanderingAnimalAIState : IAIState
    {
        private Vector3 targetLocation;
        private WanderingState wanderingState;
        private float remainingWanderTime;

        public WanderingAnimalAIState()
        {
            this.targetLocation = Vector3.zero;
            this.wanderingState = WanderingState.IDLE;
        }

        public enum WanderingState
        {
            WANDER,
            MOVING,
            IDLE
        }

        public void SetTargetLocation(Vector3 targetLocation)
        {
            this.targetLocation = targetLocation;
        }

        public Vector3 GetTargetLocation()
        {
            return this.targetLocation;
        }

        public void SetWanderingState(WanderingState wanderingState)
        {
            this.wanderingState = wanderingState;
        }

        public WanderingState GetWanderingState() 
        {
            return this.wanderingState;
        }

        public void SetRemainingWanderTime(float remainingWanderTime) 
        {
            this.remainingWanderTime = remainingWanderTime;
        }

        public float GetRemainingWanderTime()
        {
            return this.remainingWanderTime;
        }
    }
}
