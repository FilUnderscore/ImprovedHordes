using ImprovedHordes.Core.AI;
using ImprovedHordes.POI;
using UnityEngine;

namespace ImprovedHordes.Wandering.Enemy
{
    public sealed class WanderingEnemyAIState : IAIState
    {
        private WorldPOIScanner.POIZone targetZone;
        private Vector3? targetLocation;
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

        public void SetTargetZone(WorldPOIScanner.POIZone targetZone)
        {
            this.targetZone = targetZone;
        }
        
        public void SetTargetLocation(Vector3 targetLocation)
        {
            this.targetLocation = targetLocation;
        }

        public void SetWanderingState(WanderingState wanderingState) 
        {
            this.wanderingState = wanderingState;
        }

        public WorldPOIScanner.POIZone GetTargetZone()
        {
            return this.targetZone;
        }

        public Vector3? GetTargetLocation()
        {
            return this.targetLocation;
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
