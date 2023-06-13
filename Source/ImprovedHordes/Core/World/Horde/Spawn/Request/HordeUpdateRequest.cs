using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public sealed class HordeUpdateRequest : AsyncMainThreadRequest
    {
        private readonly List<HordeClusterEntity> entities = new List<HordeClusterEntity>();

        private Vector3 position;
        private readonly List<HordeClusterEntity> deadEntities = new List<HordeClusterEntity>();

        public HordeUpdateRequest(WorldHorde horde)
        {
            foreach (var cluster in horde.GetClusters())
            {
                foreach (var entity in cluster.GetEntities())
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

        public override void TickExecute(float dt)
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
