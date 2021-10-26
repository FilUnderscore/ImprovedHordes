namespace ImprovedHordes.Horde.AI.Events
{
    public class EntityDespawnedEvent
    {
        public readonly HordeAIEntity entity;

        public EntityDespawnedEvent(HordeAIEntity entity)
        {
            this.entity = entity;
        }
    }
}
