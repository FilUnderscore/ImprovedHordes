using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public abstract class HordeSpawnRequest
    {
        private ManualResetEventSlim slim = new ManualResetEventSlim(false);

        /// <summary>
        /// Execute per tick on main thread.
        /// </summary>
        public abstract void TickExecute();

        /// <summary>
        /// Is the request fulfilled? If so, notify waiting threads.
        /// </summary>
        /// <returns></returns>
        public abstract bool IsDone();

        public void Notify()
        {
            this.slim.Set();
        }

        public void Wait()
        {
            this.slim.Wait();
        }

        public void Dispose()
        {
            this.slim.Dispose();
        }
    }

    public sealed class HordeEntitySpawnRequest : HordeSpawnRequest
    {
        private readonly HordeEntityGenerator generator;
        private readonly Vector3 location;
        private readonly int size;

        private int index;
        private readonly List<EntityAlive> entities;

        public HordeEntitySpawnRequest(IHorde horde, PlayerHordeGroup playerGroup, Vector3 location, float density)
        {
            this.generator = horde.GetEntityGenerator();
            this.location = location;
            this.size = this.generator.DetermineEntityCount(playerGroup, density);

            this.index = 0;
            this.entities = new List<EntityAlive>();
        }

        public override void TickExecute()
        {
            if (GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(location, 0, 20, 20, true, out Vector3 spawnLocation, false))
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

    public sealed class HordeDespawnRequest : HordeSpawnRequest
    {
        private readonly Queue<EntityAlive> entities = new Queue<EntityAlive>();

        public HordeDespawnRequest(List<EntityAlive> entities)
        {
            foreach(EntityAlive entity in entities) 
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
            EntityAlive entity = this.entities.Dequeue();
            GameManager.Instance.World.RemoveEntity(entity.entityId, EnumRemoveEntityReason.Killed);
        }
    }

    public sealed class HordePositionUpdateRequest : HordeSpawnRequest
    {
        private readonly List<EntityAlive> entities;

        private Vector3? position;
        private readonly List<EntityAlive> deadEntities = new List<EntityAlive>();

        public HordePositionUpdateRequest(List<EntityAlive> entities)
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
                this.position += entity.position;

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

        public List<EntityAlive> GetDead()
        {
            return this.deadEntities;
        }
    }
}
