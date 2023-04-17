using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Horde.AI;
using System;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public sealed class WorldHordeSpawner
    {
        private readonly WorldHordeTracker hordeTracker;

        public WorldHordeSpawner(WorldHordeTracker hordeTracker)
        {
            this.hordeTracker = hordeTracker;
        }

        public void Spawn<Horde, HordeSpawn>(HordeSpawn spawn, params AICommand[] commands) where Horde : IHorde where HordeSpawn : IHordeSpawn
        {
            this.Spawn<Horde, HordeSpawn>(spawn, 1.0f, commands);
        }

        public void Spawn<Horde, HordeSpawn>(HordeSpawn spawn, float density, params AICommand[] commands) where Horde : IHorde where HordeSpawn : IHordeSpawn
        {
            Horde horde = Activator.CreateInstance<Horde>();

            Vector2 surfaceSpawnLocation = spawn.DetermineSurfaceLocation();
            float surfaceSpawnHeight = GameManager.Instance.World.GetHeightAt(surfaceSpawnLocation.x, surfaceSpawnLocation.y) + 1.0f;

            Vector3 spawnLocation = new Vector3(surfaceSpawnLocation.x, surfaceSpawnHeight, surfaceSpawnLocation.y);
            this.hordeTracker.Add(new WorldHorde(spawnLocation, horde, density, commands));
        }
    }
}