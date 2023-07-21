using UnityEngine;

namespace ImprovedHordes.Core.Abstractions.World
{
    public interface IEntitySpawner
    {
        bool TrySpawnAt(int entityClassId, Vector3 location, out IEntity entity);
        bool TrySpawnAt(int entityClassId, int entityId, Vector3 location, out IEntity entity);
    }
}
