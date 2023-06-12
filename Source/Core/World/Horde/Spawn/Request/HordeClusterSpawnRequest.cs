using ImprovedHordes.Core.Abstractions;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public sealed class HordeClusterSpawnRequest : IMainThreadRequest
    {
        private readonly IEntitySpawner spawner;

        private readonly WorldHorde horde;
        private readonly HordeSpawnData spawnData;

        private readonly HordeCluster cluster;
        private readonly HordeEntityGenerator generator;

        private readonly int size;
        private int index;

        private readonly Action<IEntity> onSpawnAction;
        private readonly GameRandom random;

        public HordeClusterSpawnRequest(IEntitySpawner spawner, WorldHorde horde, HordeSpawnData spawnData, HordeCluster cluster, PlayerHordeGroup playerGroup, Action<IEntity> onSpawnAction)
        {
            this.spawner = spawner;

            this.horde = horde;
            this.spawnData = spawnData;

            this.cluster = cluster;
            this.generator = DetermineEntityGenerator(playerGroup);

            this.size = this.generator.DetermineEntityCount(cluster.GetDensity());
            this.index = 0;

            this.onSpawnAction = onSpawnAction;
            this.random = GameRandomManager.Instance.CreateGameRandom(GameManager.Instance.World.Seed + this.cluster.GetHashCode()); // Allocate a random for consistent hordes using a predictable seed (hash code in this case).
        }

        private HordeEntityGenerator DetermineEntityGenerator(PlayerHordeGroup playerGroup)
        {
            HordeEntityGenerator generator = this.cluster.GetPreviousHordeEntityGenerator();

            if(generator == null || !generator.IsStillValidFor(playerGroup)) 
            {
                return this.cluster.GetHorde().CreateEntityGenerator(playerGroup);
            }
            else
            {
                generator.SetPlayerGroup(playerGroup);
                return generator;
            }
        }

        public bool IsDone()
        {
            return this.index >= this.size;
        }

        public void OnCleanup()
        {
            GameRandomManager.Instance.FreeGameRandom(this.random);
        }

        public void TickExecute(float dt)
        {
            if (!GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(this.horde.GetLocation(), 0, this.spawnData.SpreadDistanceLimit, -1, false, out Vector3 spawnLocation, false))
            {
                Log.Warning($"[Improved Hordes] Bad spawn request for horde at {this.horde.GetLocation()}");
                //TODO cancel spawn if player is too far?

                return;
            }

            this.onSpawnAction.Invoke(spawner.SpawnAt(this.generator.GetEntityClassId(this.random), spawnLocation));
            this.index++;
        }

        public int GetCount()
        {
            return this.size;
        }

        public int GetRemaining()
        {
            return this.size - this.index;
        }
    }
}
