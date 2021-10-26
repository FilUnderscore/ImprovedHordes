namespace ImprovedHordes.Horde.AI.Events
{
    public class EntityKilledEvent
    {
        public readonly HordeAIEntity entity;
        public readonly EntityAlive killer;

        public EntityKilledEvent(HordeAIEntity entity)
        {
            this.entity = entity;

            if(HordeManager.Instance.AIManager.entityKilledQueue.ContainsKey(this.entity.entity))
            {
                Entity killer = HordeManager.Instance.AIManager.entityKilledQueue[this.entity.entity];
                HordeManager.Instance.AIManager.entityKilledQueue.Remove(this.entity.entity);

                if (killer is EntityAlive)
                    this.killer = killer as EntityAlive;
            }
        }
    }
}
