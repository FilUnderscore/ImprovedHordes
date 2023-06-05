namespace ImprovedHordes.Source.Core.Horde.World.Spawn
{
    public readonly struct HordeSpawnData
    {
        public readonly int SpreadDistanceLimit;

        public HordeSpawnData(int spreadDistanceLimit)
        {
            this.SpreadDistanceLimit = spreadDistanceLimit;
        }
    }
}
