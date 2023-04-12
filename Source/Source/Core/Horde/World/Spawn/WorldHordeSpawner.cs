using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Scout;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public void Update()
        {
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