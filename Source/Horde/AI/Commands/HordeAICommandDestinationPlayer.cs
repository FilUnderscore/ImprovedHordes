using UnityEngine;

namespace ImprovedHordes.Horde.AI.Commands
{
    public class HordeAICommandDestinationPlayer : HordeAICommandDestination
    {
        public const int PLAYER_DISTANCE_TOLERANCE = 6;

        private readonly EntityPlayer player;

        public HordeAICommandDestinationPlayer(EntityPlayer player) : base(player.position, PLAYER_DISTANCE_TOLERANCE)
        {
            this.player = player;
        }

        public override void Execute(double dt, EntityAlive alive)
        {
            base.Execute(dt, alive);

            this.targetPosition = player.position;
        }
    }
}
