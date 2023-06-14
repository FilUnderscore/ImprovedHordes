using ImprovedHordes.Core.Abstractions.World;
using UnityEngine;

namespace ImprovedHordes.Implementations.World
{
    public sealed class ImprovedHordesEntitySpawner : IEntitySpawner
    {
        public IEntity SpawnAt(int entityClassId, Vector3 location)
        {
            return SpawnAt(entityClassId, EntityFactory.nextEntityID++, location);
        }

        public IEntity SpawnAt(int entityClassId, int entityId, Vector3 location)
        {
            if (GameManager.Instance.World.GetEntity(entityId) != null)
                entityId = EntityFactory.nextEntityID++;

            EntityAlive entity = EntityFactory.CreateEntity(entityClassId, entityId, location, Vector3.zero) as EntityAlive;

            if(entity != null)
            {
                GameManager.Instance.World.SpawnEntityInWorld(entity);

                entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);
                
                if (entity is EntityEnemy enemy)
                    enemy.IsHordeZombie = true;

                entity.bIsChunkObserver = true;
                entity.IsBloodMoon = false;
#if DEBUG
                entity.AddNavObject("ih_horde_zombie_debug", "", "");
#endif
            }

            return new ImprovedHordesEntity(entity);
        }
    }
}
