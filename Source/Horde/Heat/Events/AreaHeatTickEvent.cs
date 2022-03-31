namespace ImprovedHordes.Horde.Heat.Events
{
    public class AreaHeatTickEvent
    {
        public readonly Vector2i chunk;
        public readonly float heat;
        
        public AreaHeatTickEvent(Vector2i chunk, float heat)
        {
            this.chunk = chunk;
            this.heat = heat;
        }
    }
}