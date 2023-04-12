using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public sealed class RandomHordeSpawn : IHordeSpawn
    {
        public Vector2 DetermineSurfaceLocation()
        {
            if(!GameManager.Instance.World.GetWorldExtent(out var minSize, out var maxSize))
            {
                return Vector2.zero;
            }

            float x = GameManager.Instance.World.GetGameRandom().RandomRange(maxSize.x - minSize.x) + minSize.x;
            float z = GameManager.Instance.World.GetGameRandom().RandomRange(maxSize.y - minSize.y) + minSize.y;

            return new Vector2(x, z);
        }
    }
}
