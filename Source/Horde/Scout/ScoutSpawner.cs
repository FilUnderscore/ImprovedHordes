using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Horde.Scout
{
    public class ScoutSpawner : HordeSpawner
    {
        private const int CHUNK_RADIUS = 3;

        // TODO
        public ScoutSpawner() : base(null)
        {

        }

        public override int GetGroupDistance()
        {
            return CHUNK_RADIUS * 16;
        }

        public override bool GetSpawnPosition(PlayerHordeGroup playerHordeGroup, out Vector3 spawnPosition, out Vector3 targetPosition)
        {
            throw new NotImplementedException();
        }

        public override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            throw new NotImplementedException();
        }
    }
}
