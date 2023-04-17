using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Threading;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public sealed class HordeSpawnRequest : BlockingMainThreadRequest
    {
        private class ClusterSpawn
        {
            public readonly HordeCluster cluster;
            public readonly HordeEntityGenerator generator;

            public readonly int size;
            public int index;

            public ClusterSpawn(HordeCluster cluster, PlayerHordeGroup playerGroup)
            {
                this.cluster = cluster;
                this.generator = cluster.GetHorde().GetEntityGenerator();

                this.size = this.generator.DetermineEntityCount(playerGroup, cluster.GetDensity());
                this.index = 0;

                cluster.SetMaxEntityCount(this.size);
            }
        }

        private readonly WorldHorde horde;
        private readonly Dictionary<HordeCluster, ClusterSpawn> clusterSpawnData;

        private int clustersDone = 0;
        private readonly Dictionary<HordeCluster, List<EntityAlive>> entities;

        public HordeSpawnRequest(WorldHorde horde, List<HordeCluster> clusters, PlayerHordeGroup playerGroup)
        {
            this.horde = horde;
            this.clusterSpawnData = new Dictionary<HordeCluster, ClusterSpawn>();
            this.entities = new Dictionary<HordeCluster, List<EntityAlive>>();

            this.DetermineClusterSpawnData(clusters, playerGroup);
        }

        private void DetermineClusterSpawnData(List<HordeCluster> clusters, PlayerHordeGroup playerGroup)
        {
            foreach(var cluster in clusters)
            {
                clusterSpawnData.Add(cluster, new ClusterSpawn(cluster, playerGroup));
                entities.Add(cluster, new List<EntityAlive>());
            }
        }

        public override void TickExecute()
        {
            int clustersDone = 0;

            foreach(var clusterSpawn in clusterSpawnData.Values)
            {
                if(clusterSpawn.index >= clusterSpawn.size)
                {
                    clustersDone++;
                    continue;
                }

                if (GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(this.horde.GetLocation(), 0, 40, 40, true, out Vector3 spawnLocation, false))
                    this.entities[clusterSpawn.cluster].Add(clusterSpawn.generator.GenerateEntity(spawnLocation));

                clusterSpawn.index++;
            }

            this.clustersDone = clustersDone;
        }

        public override bool IsDone()
        {
            return this.clustersDone >= this.clusterSpawnData.Count;
        }

        public Dictionary<HordeCluster, List<EntityAlive>> GetEntities()
        {
            return this.entities;
        }
    }

    public sealed class HordeDespawnRequest : BlockingMainThreadRequest
    {
        private readonly Queue<HordeEntity> entities = new Queue<HordeEntity>();

        public HordeDespawnRequest(List<HordeEntity> entities)
        {
            foreach(HordeEntity entity in entities) 
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
            HordeEntity entity = this.entities.Dequeue();
            GameManager.Instance.World.RemoveEntity(entity.GetEntity().entityId, EnumRemoveEntityReason.Killed);
        }
    }

    public sealed class HordeUpdateRequest : BlockingMainThreadRequest
    {
        private readonly List<HordeEntity> entities;

        private Vector3? position;
        private readonly List<HordeEntity> deadEntities = new List<HordeEntity>();

        public HordeUpdateRequest(List<HordeEntity> entities)
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

        public List<HordeEntity> GetDead()
        {
            return this.deadEntities;
        }
    }
}
