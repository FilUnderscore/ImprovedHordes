using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Spawn
{
    public sealed class PlayerHordeSpawn : IHordeSpawn
    {
        private readonly Vector3 playerLocation;
        private readonly float distance;

        public PlayerHordeSpawn(EntityPlayer player, float distance) : this(player.position, distance)
        {
        }

        public PlayerHordeSpawn(Vector3 playerLocation, float distance)
        {
            this.playerLocation = playerLocation;
            this.distance = distance;
        }

        public Vector2 DetermineSurfaceLocation()
        {
            return FindFarthestDirectionalSpawn();
        }

        private Vector2 FindFarthestDirectionalSpawn()
        {
            float thetaRandomnessFactor = GameManager.Instance.World.GetGameRandom().RandomRange(0, 2 * Mathf.PI); // Center of horde.

            return new Vector2(this.playerLocation.x + this.distance * Mathf.Cos(thetaRandomnessFactor), 
                this.playerLocation.z + this.distance * Mathf.Sin(thetaRandomnessFactor));
        }
    }
}
