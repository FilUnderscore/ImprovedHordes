using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Scout;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ConcurrentQueue<HordeSpawnRequest> requests = new ConcurrentQueue<HordeSpawnRequest>();

        // Private
        private readonly List<HordeSpawnRequest> requestsBeingProcessed = new List<HordeSpawnRequest>();
        private readonly List<HordeSpawnRequest> requestsToRemove = new List<HordeSpawnRequest>();

        public void RequestAndWait(HordeSpawnRequest request)
        {
            requests.Enqueue(request);

            request.Wait();
            request.Dispose();
        }

        private void ProcessSpawnRequests()
        {
            while (requests.TryDequeue(out HordeSpawnRequest request))
            {
                requestsBeingProcessed.Add(request);
            }

            foreach (var request in requestsBeingProcessed)
            {
                if (!request.IsDone())
                {
                    request.TickExecute();
                }
                else
                {
                    requestsToRemove.Add(request);
                }
            }

            foreach (var request in requestsToRemove)
            {
                requestsBeingProcessed.Remove(request);
                request.Notify();
            }
            requestsToRemove.Clear();
        }

        public void Update()
        {
            this.ProcessSpawnRequests();
            this.PopulateWorldHordes();
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
            this.worldHordeClusterTracker.Add(new HordeCluster(horde, spawnLocation, density));
        }

        private void PopulateWorldHordes() 
        {
            if(this.worldHordeClusterTracker.GetClusterCount() < 2000)
            {
                this.Spawn<ScoutHorde, RandomHordeSpawn>(new RandomHordeSpawn(), 1.0f);
            }
        }
    }
}