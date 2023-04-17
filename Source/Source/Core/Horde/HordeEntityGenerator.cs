using ImprovedHordes.Source.Core.Horde.World;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde
{
    public abstract class HordeEntityGenerator
    {
        protected readonly PlayerHordeGroup playerGroup;

        public HordeEntityGenerator(PlayerHordeGroup playerGroup)
        {
            this.playerGroup = playerGroup;
        }

        public abstract int GetEntityId();

        public EntityAlive GenerateEntity(Vector3 spawnPosition)
        {
            int entityId = GetEntityId();
            EntityAlive entity = EntityFactory.CreateEntity(entityId, spawnPosition) as EntityAlive;

            if(entity != null)
            {
                GameManager.Instance.World.SpawnEntityInWorld(entity);

                entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);

                if (entity is EntityEnemy enemy)
                    enemy.IsHordeZombie = true;

                entity.bIsChunkObserver = true;
                entity.IsBloodMoon = false;

#if DEBUG
                entity.AddNavObject("ih_horde_zombie_debug", "");
#endif
            }

            return entity;
        }

        public abstract int DetermineEntityCount(float density);
    }
}
