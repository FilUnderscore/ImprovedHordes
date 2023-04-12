using ImprovedHordes.Source.Core.Horde.World.Spawn;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public class HordeCluster
    {
        private readonly IHorde horde;
        private Vector3 location;
        private float density;
        private float densityPerEntity;

        private List<EntityAlive> entities = new List<EntityAlive>();
        private bool spawned;

        public HordeCluster(IHorde horde, Vector3 location, float density)
        {
            this.horde = horde;
            this.location = location;
            this.density = density;

            this.spawned = false;
        }

        public IHorde GetHorde()
        {
            return this.horde;
        }

        public Vector3 GetLocation()
        {
            return this.location;
        }

        public float GetDensity() 
        {
            return this.density;
        }

        public void Spawn(PlayerHordeGroup group)
        {
            if(ImprovedHordesCore.TryGetInstance(out var instance))
            {
                var request = new HordeSpawnRequest(horde, group, location, density);
                instance.GetMainThreadRequestProcessor().RequestAndWait(request);

                this.entities.AddRange(request.GetEntities());
                this.densityPerEntity = this.density / this.entities.Count;

                spawned = true;
            }
        }

        public void Despawn()
        {
            if(ImprovedHordesCore.TryGetInstance(out var instance))
            {
                instance.GetMainThreadRequestProcessor().RequestAndWait(new HordeDespawnRequest(this.entities));
                this.entities.Clear();

                spawned = false;
            }
        }

        public void UpdatePosition()
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                var request = new HordeUpdateRequest(this.entities);
                instance.GetMainThreadRequestProcessor().RequestAndWait(request);

                this.location = request.GetPosition();
                request.GetDead().ForEach(deadEntity =>
                {
                    this.entities.Remove(deadEntity);
                    this.density -= this.densityPerEntity;
                });
            }
        }

        public bool IsSpawned()
        {
            return this.spawned;
        }

        public bool IsDead()
        {
            return this.density <= 0.0f;
        }
    }
}
