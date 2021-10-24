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
        public bool hasEndPosition = false;

        public Scout(HordeAIEntity aiEntity)
        {
            this.aiEntity = aiEntity;
        }

        public void Interrupt(Vector3 newPosition)
        {
            this.targetPosition = newPosition;
            
            if(!this.hasEndPosition)
                this.CalculateEndPosition();

            this.aiEntity.InterruptWithNewCommands(new HordeAICommandDestination(newPosition, 10), new HordeAICommandWander(WANDER_TIME), new HordeAICommandDestination(endPosition, DISTANCE_TOLERANCE));
        }

        public void CalculateEndPosition()
        {
            if (this.hasEndPosition)
                return;

            var random = HordeManager.Instance.Random;
            var radius = random.RandomRange(80, 12 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance));
            var randomOnCircle = random.RandomOnUnitCircle;

            this.endPosition = this.targetPosition + new Vector3(randomOnCircle.x, 0, randomOnCircle.y) * radius;
            var result = Utils.GetSpawnableY(ref this.endPosition);

            if (!result)
                CalculateEndPosition();

            this.hasEndPosition = true;
            this.aiEntity.commands.Add(new HordeAICommandDestination(this.endPosition, 10));
        }
    }
}
