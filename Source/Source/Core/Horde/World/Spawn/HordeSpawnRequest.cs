using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public abstract class HordeSpawnRequest
    {
        private readonly ManualResetEventSlim slim = new ManualResetEventSlim(false);

        /// <summary>
        /// Execute per tick on main thread.
        /// </summary>
        public abstract void TickExecute();

        /// <summary>
        /// Is the request fulfilled? If so, notify waiting threads.
        /// </summary>
        /// <returns></returns>
        public abstract bool IsDone();

        /// <summary>
        /// Notify waiting thread to continue execution.
        /// </summary>
        public void Notify()
        {
            this.slim.Set();
        }

        /// <summary>
        /// Called by waiting thread.
        /// </summary>
        public void Wait()
        {
            this.slim.Wait();
        }

        /// <summary>
        /// Called by waiting thread to dispose of sync object.
        /// </summary>
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
        private readonly EntityAlive[] entities;

        public HordeEntitySpawnRequest(IHorde horde, Vector3 location, int size)
        {
            this.generator = horde.GetEntityGenerator();
            this.location = location;
            this.size = size;

            this.index = 0;
            this.entities = new EntityAlive[size];
        }

        public override void TickExecute()
        {
            Vector2 randomInsideCircle = GameManager.Instance.World.GetGameRandom().RandomInsideUnitCircle;
            float circleSize = 10.0f;

            Vector2 surfaceSpawnLocation = new Vector2(location.x, location.z) + randomInsideCircle * circleSize;
            float surfaceSpawnHeight = GameManager.Instance.World.GetHeightAt(surfaceSpawnLocation.x, surfaceSpawnLocation.y) + 1.0f;

            Vector3 spawnLocation = new Vector3(surfaceSpawnLocation.x, surfaceSpawnHeight, surfaceSpawnLocation.y);
            this.entities[index++] = generator.GenerateEntity(spawnLocation);
        }

        public override bool IsDone()
        {
            return this.index >= this.size;
        }

        public EntityAlive[] GetEntities()
        {
            return this.entities;
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

            for(int i = 0; i < 2; i++)
                entity.Kill(DamageResponse.New(true));
        }
    }
}
