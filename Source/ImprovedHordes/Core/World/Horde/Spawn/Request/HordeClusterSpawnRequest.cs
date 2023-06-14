using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.World.Horde.Cluster;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public readonly struct HordeClusterSpawnRequest
    {
        public readonly WorldHorde Horde;
        public readonly HordeCluster Cluster;
        public readonly PlayerHordeGroup PlayerGroup;
        public readonly HordeSpawnData SpawnData;

        public readonly ThreadSubscriber<HordeClusterSpawnState> State;

        public HordeClusterSpawnRequest(WorldHorde horde, HordeCluster cluster, PlayerHordeGroup playerGroup, HordeSpawnData spawnData, ThreadSubscriber<HordeClusterSpawnState> state)
        {
            this.Horde = horde;
            this.Cluster = cluster;
            this.PlayerGroup = playerGroup;
            this.SpawnData = spawnData;
            this.State = state;
        }
    }
}
