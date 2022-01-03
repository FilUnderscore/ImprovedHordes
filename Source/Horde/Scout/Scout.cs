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
        private readonly ScoutManager manager;
        public HordeAIEntity aiEntity;
        public HordeAIHorde aiHorde;

        public bool hasEndPosition = false;

        public EntityPlayer killer = null;
        public EScoutState state = EScoutState.ALIVE;

        public Scout(ScoutManager manager, HordeAIEntity aiEntity, HordeAIHorde aiHorde)
        {
            this.manager = manager;
            this.aiEntity = aiEntity;
            this.aiHorde = aiHorde;
        }

        private int calledScoutWanderAttempts = 0;
        private const int maxCalledScoutWanderAttempts = 3;

        public void Interrupt(Vector3 newPosition, float value)
        {
            if (this.state != EScoutState.ALIVE)
                return;

            HordeAICommand currentCommand = this.aiEntity.GetCurrentCommand();

            if(currentCommand == null) // Only called when scout zombie horde spawns a screamer that runs out of instructions. Give it new instructions.
            {
                if (calledScoutWanderAttempts < maxCalledScoutWanderAttempts)
                {
                    float wanderTime = 90f + this.manager.manager.Random.RandomFloat * 4f;
                    this.aiEntity.InterruptWithNewCommands(new HordeAICommandWander(wanderTime));

                    calledScoutWanderAttempts++;
                }
                else
                {
                    const int DEST_RADIUS = 10;

                    Vector2 randomPositionOnCircle = this.manager.manager.Random.RandomOnUnitCircle;
                    float magnitude = this.manager.CHUNK_RADIUS * 16;

                    Vector3 despawnLocation = new Vector3(magnitude * randomPositionOnCircle.x + newPosition.x, 0, magnitude * randomPositionOnCircle.y + newPosition.z);
                    Utils.GetSpawnableY(ref despawnLocation);

                    this.aiEntity.InterruptWithNewCommands(new HordeAICommandDestination(despawnLocation, DEST_RADIUS));
                    this.aiEntity.despawnOnCompletion = true;
                }

                HordeAICommandScout newScoutCommand = new HordeAICommandScout(this.manager, this.aiEntity);

                this.aiEntity.InterruptWithNewCommands(newScoutCommand);

                return;
            }

            if(currentCommand != null && !(currentCommand is HordeAICommandScout))
            {
                Warning("[Scout] Current AI command is not scout.");

                return;
            }

            HordeAICommandScout scoutCommand = (HordeAICommandScout) currentCommand;
            scoutCommand.UpdateTarget(newPosition, value);
        }
    }

    public enum EScoutState
    {
        ALIVE,
        DEAD,
        DESPAWNED
    }
}
