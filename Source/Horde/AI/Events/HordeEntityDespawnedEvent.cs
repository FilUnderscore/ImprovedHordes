namespace ImprovedHordes.Horde.AI.Events
{
    public class HordeEntityDespawnedEvent
    {
        public readonly HordeAIEntity entity;

        public HordeEntityDespawnedEvent(HordeAIEntity entity)
        {
            this.entity = entity;
        }
    }
}
