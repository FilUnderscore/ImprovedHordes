namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public readonly struct HordeClusterSpawnState
    {
        public readonly int spawned;
        public readonly int remaining;

        public readonly bool complete;

        public HordeClusterSpawnState(int spawned, int remaining, bool complete)
        {
            this.spawned = spawned;
            this.remaining = remaining;
            this.complete = complete;
        }
    }
}
