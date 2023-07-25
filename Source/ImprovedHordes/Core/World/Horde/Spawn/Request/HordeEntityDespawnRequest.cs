using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public sealed class HordeEntityDespawnRequest : IMainThreadRequest
    {
        private readonly HordeClusterEntity entity;
        private readonly Action<IEntity> onDespawn;

        public HordeEntityDespawnRequest(HordeClusterEntity entity, Action<IEntity> onDespawn)
        {
            this.entity = entity;
            this.onDespawn = onDespawn;
        }

        public bool IsDone()
        {
            return !this.entity.IsAwaitingSpawnStateChange() && !this.entity.IsSpawned();
        }

        public void OnCleanup()
        {
        }

        public void TickExecute(float dt)
        {
            this.onDespawn?.Invoke(this.entity.GetEntity());
            this.entity.Despawn();
        }
    }
}
