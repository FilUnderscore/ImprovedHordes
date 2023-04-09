using ImprovedHordes.Source.Core.Horde.World;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde
{
    public abstract class HordeEntityGenerator
    {
        public HordeEntityGenerator()
        {

        }

        public EntityAlive GenerateEntity(Vector3 spawnPosition)
        {
            int entityId = EntityClass.FromString("zombieSpider"); // TODO
            EntityAlive entity = EntityFactory.CreateEntity(entityId, spawnPosition) as EntityAlive;

            if(entity != null)
            {
                GameManager.Instance.World.SpawnEntityInWorld(entity);

                entity.SetSpawnerSource(EnumSpawnerSource.Dynamic);

                if (entity is EntityEnemy enemy)
                    enemy.IsHordeZombie = true;

                entity.bIsChunkObserver = true;
                entity.IsBloodMoon = false;

                entity.AddNavObject("ih_horde_zombie_debug", "");

                Log.Out("Spawned horde zombie.");
            }

            return entity;
        }

        public int DetermineEntityCount(PlayerHordeGroup playerGroup, float density)
        {
            Log.Out("Entity density: " + density);
            return Mathf.CeilToInt(10 * density);
        }
    }
}
