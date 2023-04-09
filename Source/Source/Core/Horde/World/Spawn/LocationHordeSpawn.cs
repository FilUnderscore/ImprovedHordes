using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public sealed class LocationHordeSpawn : IHordeSpawn
    {
        private Vector2 location;

        public LocationHordeSpawn(Vector2 location)
        {
            this.location = location;
        }

        public Vector2 DetermineSurfaceLocation()
        {
            return this.location;
        }
    }
}