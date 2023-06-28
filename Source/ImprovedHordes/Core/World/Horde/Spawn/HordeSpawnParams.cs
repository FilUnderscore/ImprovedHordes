namespace ImprovedHordes.Core.World.Horde.Spawn
{
    public readonly struct HordeSpawnParams
    {
        public readonly int SpreadDistanceLimit;

        public HordeSpawnParams(int spreadDistanceLimit)
        {
            this.SpreadDistanceLimit = spreadDistanceLimit;
        }
    }
}
