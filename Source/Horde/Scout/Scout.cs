using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;
using ImprovedHordes.Horde.Wandering.AI.Commands;

namespace ImprovedHordes.Horde.Scout
{
    public class Scout
    {
        private const int DISTANCE_TOLERANCE = 10;
        private const float WANDER_TIME = 100.0f;

        public HordeAIEntity aiEntity;
        public Vector3 targetPosition;
        public Vector3 endPosition;

        public Scout(HordeAIEntity aiEntity, Vector3 targetPosition, Vector3 endPosition)
        {
            this.aiEntity = aiEntity;
            this.targetPosition = targetPosition;
            this.endPosition = endPosition;
        }

        public void Interrupt(Vector3 newPosition)
        {
            this.aiEntity.InterruptWithNewCommands(new HordeAICommandDestination(newPosition, 10), new HordeAICommandWander(WANDER_TIME), new HordeAICommandDestination(endPosition, DISTANCE_TOLERANCE));
        }
    }
}
