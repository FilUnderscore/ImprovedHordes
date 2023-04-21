using ImprovedHordes.Source.Core.Threading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class HordeClusterSpawnRequest : IMainThreadRequest
    {
        private readonly WorldHorde horde;
        private readonly HordeCluster cluster;
        private readonly HordeEntityGenerator generator;

        private readonly int size;
        private int index;

        private readonly Action<EntityAlive> onSpawnAction;
        
        public HordeClusterSpawnRequest(WorldHorde horde, HordeCluster cluster, PlayerHordeGroup playerGroup, Action<EntityAlive> onSpawnAction)
        {
            this.horde = horde;
            this.cluster = cluster;
            this.generator = DetermineEntityGenerator(playerGroup);

            this.size = this.generator.DetermineEntityCount(cluster.GetDensity());
            this.index = 0;

            this.onSpawnAction = onSpawnAction;
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
        }

        public void TickExecute()
        {
            if (!GameManager.Instance.World.GetRandomSpawnPositionMinMaxToPosition(this.horde.GetLocation(), 0, 40, -1, false, out Vector3 spawnLocation, false))
            {
                Log.Warning($"[Improved Hordes] Bad spawn request for horde at {this.horde.GetLocation()}");
                return;
            }

            this.onSpawnAction.Invoke(this.generator.GenerateEntity(spawnLocation));
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


    public sealed class HordeDespawnRequest : BlockingMainThreadRequest
    {
        private readonly Queue<HordeClusterEntity> entities = new Queue<HordeClusterEntity>();

        public HordeDespawnRequest(WorldHorde horde)
        {
            foreach(var cluster in horde.GetClusters())
            {
                foreach(var entity in cluster.GetEntities())
                {
                    entities.Enqueue(entity);
                }
            }
        }

        public override bool IsDone()
        {
            return this.entities.Count == 0;
        }

        public override void TickExecute()
        {
            if (this.entities.Count == 0)
            {
                Log.Warning("[Improved Hordes] Tried to despawn horde entities but no entities were spawned.");
                return;
            }

            HordeClusterEntity entity = this.entities.Dequeue();
            GameManager.Instance.World.RemoveEntity(entity.GetEntity().entityId, EnumRemoveEntityReason.Killed);
            entity.GetCluster().RemoveEntity(entity);
        }
    }

    public sealed class HordeUpdateRequest : BlockingMainThreadRequest
    {
        private readonly List<HordeClusterEntity> entities = new List<HordeClusterEntity>();

        private Vector3 position;
        private readonly List<HordeClusterEntity> deadEntities = new List<HordeClusterEntity>();

        public HordeUpdateRequest(WorldHorde horde)
        {
            foreach(var cluster in horde.GetClusters())
            {
                foreach(var entity in cluster.GetEntities())
                {
                    this.entities.Add(entity);
                }
            }

            this.position = horde.GetLocation();
        }

        public override bool IsDone()
        {
            return true;
        }

        public override void TickExecute()
        {
            if (this.entities.Count == 0)
                return;

            this.position = Vector3.zero;

            foreach (var entity in this.entities)
            {
                this.position += entity.GetLocation();

                if (entity.IsDead())
                {
                    deadEntities.Add(entity);
                }
            }

            this.position /= this.entities.Count;
        }

        public Vector3 GetPosition()
        {
            return this.position;
        }

        public List<HordeClusterEntity> GetDead()
        {
            return this.deadEntities;
        }
    }
}
