using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public sealed class HordeEntitySpawnRequest : IMainThreadRequest
    {
        private readonly IEntitySpawner spawner;
        private readonly HordeClusterEntity entity;
        private readonly bool spawn;
        private readonly Action<IEntity> onSpawn;

        public HordeEntitySpawnRequest(IEntitySpawner spawner, HordeClusterEntity entity, bool spawn, Action<IEntity> onSpawn)
        {
            this.spawner = spawner;
            this.entity = entity;
            this.spawn = spawn;
            this.onSpawn = onSpawn;
        }

        public bool IsDone()
        {
            return !this.entity.IsAwaitingSpawnStateChange() && this.entity.IsSpawned() == spawn;
        }

        public void OnCleanup()
        {
        }

        public void TickExecute(float dt)
        {
            if (this.spawn)
            {
                this.entity.Respawn(this.spawner);

                if (this.onSpawn != null)
                    this.onSpawn(this.entity.GetEntity());
            }
            else
            {
                if (this.onSpawn != null)
                    this.onSpawn(this.entity.GetEntity());

                this.entity.Despawn();
            }
        }
    }
}
