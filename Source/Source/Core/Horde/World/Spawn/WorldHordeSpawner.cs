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
        
        // Shared
        private readonly List<HordeSpawnRequest> requests = new List<HordeSpawnRequest>();
        private readonly object requestsLock = new object();

        // Private
        private readonly List<HordeSpawnRequest> requestsToRemove = new List<HordeSpawnRequest>();

        public void Update()
        {
            if(Monitor.TryEnter(requestsLock))
            {
                foreach(var request in requests)
                {
                    if(!request.IsDone())
                    {
                        request.TickExecute();
                    }
                    else
                    {
                        request.Notify();
                        requestsToRemove.Add(request);
                    }
                }

                foreach(var requestToRemove in requestsToRemove)
                {
                    requests.Remove(requestToRemove);
                }

                requestsToRemove.Clear();

                Monitor.Exit(requestsLock);
            }
        }

        public void Spawn<Horde, HordeSpawn>(HordeSpawn spawn) where Horde : IHorde where HordeSpawn : IHordeSpawn
        {
            this.Spawn<Horde, HordeSpawn>(spawn, 1.0f);
        }

        public void Spawn<Horde, HordeSpawn>(HordeSpawn spawn, float density) where Horde : IHorde where HordeSpawn : IHordeSpawn
        {
            Horde horde = Activator.CreateInstance<Horde>();

            Vector2 surfaceSpawnLocation = spawn.DetermineSurfaceLocation();
            float surfaceSpawnHeight = GameManager.Instance.World.GetHeightAt(surfaceSpawnLocation.x, surfaceSpawnLocation.y) + 1.0f;

            Vector3 spawnLocation = new Vector3(surfaceSpawnLocation.x, surfaceSpawnHeight, surfaceSpawnLocation.y);
            this.worldHordeClusterTracker.AddHorde(horde, spawnLocation, density);
        }

        public void Request(HordeSpawnRequest request)
        {
            Monitor.Enter(this.requestsLock);
            requests.Add(request);
            Monitor.Exit(this.requestsLock);

            request.Wait();
            request.Dispose();
        }
    }
}