using UnityEngine;

namespace ImprovedHordes.Core.Abstractions
{
    public interface IEntitySpawner
    {
        IEntity SpawnAt(int entityClassId, Vector3 location);
        IEntity SpawnAt(int entityClassId, int entityId, Vector3 location);
    }
}
