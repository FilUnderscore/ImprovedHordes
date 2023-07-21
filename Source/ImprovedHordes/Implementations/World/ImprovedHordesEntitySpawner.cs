using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.POI;
using UnityEngine;

namespace ImprovedHordes.Implementations.World
{
    public sealed class ImprovedHordesEntitySpawner : IEntitySpawner
    {
        private readonly WorldPOIScanner poiScanner;
        private readonly IWorldRandom worldRandom;

        public ImprovedHordesEntitySpawner(WorldPOIScanner poiScanner, IWorldRandom worldRandom)
        {
            this.poiScanner = poiScanner;
            this.worldRandom = worldRandom;
        }

        public bool TrySpawnAt(int entityClassId, Vector3 location, out IEntity entity)
        {
            return TrySpawnAt(entityClassId, EntityFactory.nextEntityID++, location, out entity);
        }

        public bool TrySpawnAt(int entityClassId, int entityId, Vector3 location, out IEntity entity)
        {
            location = FindSpawnLocationNear(location);

            if (GameManager.Instance.World.GetEntity(entityId) != null)
                entityId = EntityFactory.nextEntityID++;

            EntityAlive entityAlive = EntityFactory.CreateEntity(entityClassId, entityId, location, Vector3.zero) as EntityAlive;

            if(entityAlive != null)
            {
                GameManager.Instance.World.SpawnEntityInWorld(entityAlive);

                entityAlive.SetSpawnerSource(EnumSpawnerSource.Dynamic);
                
                if (entityAlive is EntityEnemy enemy)
                    enemy.IsHordeZombie = true;

                entityAlive.bIsChunkObserver = true;
                entityAlive.IsBloodMoon = false;
#if DEBUG
                entityAlive.AddNavObject("ih_horde_zombie_debug", "", "");
#endif

                entity = new ImprovedHordesEntity(entityAlive);
            }
            else
            {
                entity = null;
            }

            return entity != null;
        }

        private Vector3 FindSpawnLocationNear(Vector3 location)
        {
            if (!GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(location, 0, 15, -1, true, out Vector3 spawnLocation, false))
            {
                // Check for POI
                WorldPOIScanner.POI poi = this.poiScanner.GetPOIAt(location);

                if (poi == null)
                {
                    spawnLocation = location;
                }
                else
                {
                    poi.GetLocationOutside(this.worldRandom, out Vector2 spawnLocationXZ);
                    spawnLocation = new Vector3(spawnLocationXZ.x, 0, spawnLocationXZ.y);
                }

                spawnLocation.y = GameManager.Instance.World.GetHeightAt(spawnLocation.x, spawnLocation.z) + 1.0f; // Fix entities falling off of world when getting random spawn position fails.
            }

            return spawnLocation;
        }
    }
}
