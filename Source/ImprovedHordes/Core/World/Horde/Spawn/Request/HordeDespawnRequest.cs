using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System;
using System.Collections.Generic;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public sealed class HordeDespawnRequest : IMainThreadRequest
    {
        private readonly ILogger logger;
        private readonly Queue<HordeClusterEntity> entities = new Queue<HordeClusterEntity>();

        private readonly Action onDespawnedAction;

        public HordeDespawnRequest(ILoggerFactory loggerFactory, WorldHorde horde, Action onDespawned)
        {
            this.logger = loggerFactory.Create(typeof(HordeDespawnRequest));

            foreach (var cluster in horde.GetClusters())
            {
                foreach (var entity in cluster.GetEntities())
                {
                    entities.Enqueue(entity);
                }
            }

            this.onDespawnedAction = onDespawned;
        }

        public bool IsDone()
        {
            return this.entities.Count == 0;
        }

        public void OnCleanup()
        {
            if (this.onDespawnedAction != null)
                this.onDespawnedAction.Invoke();
        }

        public void TickExecute(float dt)
        {
            if (this.entities.Count == 0)
            {
                this.logger.Warn("Tried to despawn horde entities but no entities were spawned.");
                return;
            }

            HordeClusterEntity entity = this.entities.Dequeue();

            if (entity.IsSpawned()) // Check if not already despawned.
                GameManager.Instance.World.RemoveEntity(entity.GetEntity().GetEntityId(), EnumRemoveEntityReason.Killed);

            entity.NotifyHordeDespawned();
            entity.GetCluster().RemoveEntity(entity);
        }
    }
}
