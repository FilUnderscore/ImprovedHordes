using UnityEngine;

namespace ImprovedHordes.Horde.AI.Commands
{
    public class HordeAICommandDestinationPlayer : HordeAICommandDestinationMoving
    {
        public const int PLAYER_DISTANCE_TOLERANCE = 20;

        public HordeAICommandDestinationPlayer(EntityPlayer player) : base(() => player.position, PLAYER_DISTANCE_TOLERANCE)
        {
        }
    }
}
