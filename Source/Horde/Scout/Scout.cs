using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;
using ImprovedHordes.Horde.Wandering.AI.Commands;
using ImprovedHordes.Horde.Scout.AI.Commands;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Scout
{
    public class Scout
    {
        private const int DISTANCE_TOLERANCE = 10;
        private const float WANDER_TIME = 100.0f;

        public HordeAIEntity aiEntity;
        public HordeAIHorde aiHorde;

        public bool hasEndPosition = false;

        public Scout(HordeAIEntity aiEntity, HordeAIHorde aiHorde)
        {
            this.aiEntity = aiEntity;
            this.aiHorde = aiHorde;
        }

        public void Interrupt(Vector3 newPosition, float value)
        {
            HordeAICommand currentCommand = this.aiEntity.GetCurrentCommand();

            if(currentCommand != null && !(currentCommand is HordeAICommandScout))
            {
                Warning("[Scout] Current AI command is not scout.");

                return;
            }

            HordeAICommandScout scoutCommand = (HordeAICommandScout) currentCommand;
            scoutCommand.UpdateTarget(newPosition, value);
        }
    }
}
