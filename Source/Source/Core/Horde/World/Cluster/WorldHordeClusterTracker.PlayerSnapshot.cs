using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public sealed partial class WorldHordeClusterTracker
    {
        /// <summary>
        /// Snapshot of a Player at the time of capture. Used to determine Horde difficulty.
        /// </summary>
        private sealed class PlayerSnapshot
        {
            private readonly Vector3 location;
            private readonly int gamestage;

            public PlayerSnapshot(EntityPlayer player)
            {
                this.location = player.position;
                this.gamestage = player.gameStage;
            }

            public Vector3 GetLocation()
            {
                return this.location;
            }

            public int GetGamestage()
            {
                return this.gamestage;
            }
        }
    }
}
