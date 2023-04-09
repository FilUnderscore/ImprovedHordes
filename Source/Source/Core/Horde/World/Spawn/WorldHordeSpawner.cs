using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public sealed class WorldHordeSpawner
    {
        private readonly WorldHordeClusterTracker worldHordeClusterTracker;

        public WorldHordeSpawner(WorldHordeClusterTracker worldHordeClusterTracker)
        {
            this.worldHordeClusterTracker = worldHordeClusterTracker;
        }

        private readonly List<IHordeSpawnRequest> requests = new List<IHordeSpawnRequest>();

        public void Update()
        {
            if(Monitor.TryEnter(requests))
            {
                foreach(var request in requests)
                {
                    request.Execute();
                    request.Notify();
                }

                requests.Clear();
                Monitor.Exit(requests);
            }
        }

        public void Spawn<Horde, HordeSpawn>(HordeSpawn spawn) where Horde : IHorde where HordeSpawn : IHordeSpawn
        {
            Horde horde = Activator.CreateInstance<Horde>();

            Vector2 surfaceSpawnLocation = spawn.DetermineSurfaceLocation();
            float surfaceSpawnHeight = GameManager.Instance.World.GetHeightAt(surfaceSpawnLocation.x, surfaceSpawnLocation.y) + 1.0f;

            Vector3 spawnLocation = new Vector3(surfaceSpawnLocation.x, surfaceSpawnHeight, surfaceSpawnLocation.y);
            this.worldHordeClusterTracker.AddHorde(horde, spawnLocation, 10);
        }

        public void Request(IHordeSpawnRequest request)
        {
            Monitor.Enter(this.requests);
            requests.Add(request);
            Monitor.Exit(this.requests);

            request.Wait();
            request.Dispose();
        }
    }
}