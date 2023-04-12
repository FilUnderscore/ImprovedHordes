using ImprovedHordes.Source.Core.Horde.World.Spawn;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public class HordeCluster
    {
        private readonly IHorde horde;
        private Vector3 location;
        private float density;

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
                var request = new HordeEntitySpawnRequest(horde, group, location, density);
                instance.GetHordeManager().GetSpawner().RequestAndWait(request);

                this.entities.AddRange(request.GetEntities());

                spawned = true;
            }
        }

        public void Despawn()
        {
            if(ImprovedHordesCore.TryGetInstance(out var instance))
            {
                instance.GetHordeManager().GetSpawner().RequestAndWait(new HordeDespawnRequest(this.entities));
                this.entities.Clear();

                spawned = false;
            }
        }

        public void UpdatePosition()
        {
            if (ImprovedHordesCore.TryGetInstance(out var instance))
            {
                var request = new HordePositionUpdateRequest(this.entities);
                instance.GetHordeManager().GetSpawner().RequestAndWait(request);

                this.location = request.GetPosition();
            }
        }

        public bool IsSpawned()
        {
            return this.spawned;
        }
    }
}
