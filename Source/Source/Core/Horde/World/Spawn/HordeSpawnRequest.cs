using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public interface IHordeSpawnRequest
    {
        void Execute();

        void Notify();
        void Wait();
        void Dispose();
    }

    public sealed class HordeSpawnRequest : IHordeSpawnRequest
    {
        private ManualResetEventSlim slim;

        private IHorde horde;
        private Vector3 location;
        private int size;

        private EntityAlive[] entities;

        public HordeSpawnRequest(IHorde horde, Vector3 location, int size)
        {
            this.slim = new ManualResetEventSlim(false);

            this.horde = horde;
            this.location = location;
            this.size = size;
            this.entities = new EntityAlive[size];
        }

        public void Execute()
        {
            HordeEntityGenerator generator = this.horde.GetEntityGenerator();

            for (int i = 0; i < size; i++)
            {
                Vector2 randomInsideCircle = GameManager.Instance.World.GetGameRandom().RandomInsideUnitCircle;
                float circleSize = 10.0f;

                Vector2 surfaceSpawnLocation = new Vector2(location.x, location.z) + randomInsideCircle * circleSize;
                float surfaceSpawnHeight = GameManager.Instance.World.GetHeightAt(surfaceSpawnLocation.x, surfaceSpawnLocation.y) + 1.0f;

                Vector3 spawnLocation = new Vector3(surfaceSpawnLocation.x, surfaceSpawnHeight, surfaceSpawnLocation.y);
                this.entities[i] = generator.GenerateEntity(spawnLocation);
            }
        }

        public EntityAlive[] GetEntities()
        {
            return this.entities;
        }

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

    public sealed class HordeDespawnRequest : IHordeSpawnRequest
    {
        private ManualResetEventSlim slim;
        private List<EntityAlive> entities;

        public HordeDespawnRequest(List<EntityAlive> entities)
        {
            this.slim = new ManualResetEventSlim(false);
            this.entities = entities;
        }

        public void Execute()
        {
            foreach (var entity in this.entities)
            {
                entity.Kill(DamageResponse.New(true));
                entity.Kill(DamageResponse.New(true));
            }
        }

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
}
