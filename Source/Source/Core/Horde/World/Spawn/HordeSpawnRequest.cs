using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public sealed class HordeSpawnRequest : BlockingMainThreadRequest
    {
        private readonly HordeEntityGenerator generator;
        private readonly Vector3 location;
        private readonly int size;

        private int index;
        private readonly List<EntityAlive> entities;

        public HordeSpawnRequest(IHorde horde, PlayerHordeGroup playerGroup, Vector3 location, float density)
        {
            this.generator = horde.GetEntityGenerator();
            this.location = location;
            this.size = this.generator.DetermineEntityCount(playerGroup, density);

            this.index = 0;
            this.entities = new List<EntityAlive>();
        }

        public override void TickExecute()
        {
            if (GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(location, 0, 40, 40, true, out Vector3 spawnLocation, false))
                this.entities.Add(generator.GenerateEntity(spawnLocation));

            this.index++;
        }

        public override bool IsDone()
        {
            return this.index >= this.size;
        }

        public EntityAlive[] GetEntities()
        {
            return this.entities.ToArray();
        }
    }

    public sealed class HordeDespawnRequest : BlockingMainThreadRequest
    {
        private readonly Queue<HordeClusterEntity> entities = new Queue<HordeClusterEntity>();

        public HordeDespawnRequest(List<HordeClusterEntity> entities)
        {
            foreach(HordeClusterEntity entity in entities) 
            {
                this.entities.Enqueue(entity);
            }
        }

        public override bool IsDone()
        {
            return this.entities.Count == 0;
        }

        public override void TickExecute()
        {
            HordeClusterEntity entity = this.entities.Dequeue();
            GameManager.Instance.World.RemoveEntity(entity.GetEntity().entityId, EnumRemoveEntityReason.Killed);
        }
    }

    public sealed class HordeUpdateRequest : BlockingMainThreadRequest
    {
        private readonly List<HordeClusterEntity> entities;

        private Vector3? position;
        private readonly List<HordeClusterEntity> deadEntities = new List<HordeClusterEntity>();

        public HordeUpdateRequest(List<HordeClusterEntity> entities)
        {
            this.entities = entities;
            this.position = null;
        }

        public override bool IsDone()
        {
            return this.position != null;
        }

        public override void TickExecute()
        {
            this.position = Vector3.zero;

            if (this.entities.Count == 0)
                return;

            foreach(var entity in this.entities)
            {
                this.position += entity.GetLocation();

                if(entity.IsDead())
                {
                    deadEntities.Add(entity);
                }
            }

            this.position /= this.entities.Count;
        }

        public Vector3 GetPosition()
        {
            return this.position.Value;
        }

        public List<HordeClusterEntity> GetDead()
        {
            return this.deadEntities;
        }
    }
}
