using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.Core.World.Horde.Cluster
{
    [Flags]
    public enum EHordeClusterSpawnState
    {
        SPAWNED = 1,
        SPAWNING = 2,
        DESPAWNED = 4,
        DESPAWNING = 8
    }
}
