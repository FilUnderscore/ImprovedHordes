namespace ImprovedHordes.Horde.AI.Events
{
    public class HordeEntityDespawnedEvent : EntityDespawnedEvent
    {
        public readonly HordeAIHorde horde;

        public HordeEntityDespawnedEvent(HordeAIEntity entity, HordeAIHorde horde) : base(entity)
        {
            this.horde = horde;
        }
    }
}
