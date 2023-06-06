using ImprovedHordes.Source.Core.Horde.World;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde
{
    public abstract class HordeEntityGenerator
    {
        protected PlayerHordeGroup playerGroup;

        public HordeEntityGenerator(PlayerHordeGroup playerGroup)
        {
            this.playerGroup = playerGroup;
        }

        public void SetPlayerGroup(PlayerHordeGroup playerGroup)
        {
            this.playerGroup = playerGroup;
        }

        public abstract bool IsStillValidFor(PlayerHordeGroup playerGroup);

        public abstract int GetEntityId(GameRandom random);

        public EntityAlive GenerateEntity(Vector3 spawnPosition, GameRandom random)
        {
            return GenerateEntity(GetEntityId(random), spawnPosition);
        }

        public abstract int DetermineEntityCount(float density);

        public static EntityAlive GenerateEntity(int entityId, Vector3 spawnPosition)
        {
            EntityAlive entity = EntityFactory.CreateEntity(entityId, spawnPosition) as EntityAlive;

            if (entity != null)
            {
                GameManager.Instance.World.SpawnEntityInWorld(entity);

                entity.SetSpawnerSource(EnumSpawnerSource.Unknown);

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
    }
}
