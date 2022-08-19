namespace ImprovedHordes.Horde.AI.Events
{
    public class HordeEntityKilledEvent : EntityKilledEvent
    {
        public readonly HordeAIHorde horde;

        public HordeEntityKilledEvent(HordeAIEntity entity, HordeAIHorde horde) : base(entity)
        {
            this.horde = horde;
        }
    }
}
