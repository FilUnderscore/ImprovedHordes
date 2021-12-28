namespace ImprovedHordes.Horde.AI.Events
{
    public class EntityKilledEvent
    {
        public readonly HordeAIEntity entity;
        public readonly EntityAlive killer;

        public EntityKilledEvent(HordeAIEntity entity)
        {
            this.entity = entity;

            if(ImprovedHordesManager.Instance.AIManager.entityKilledQueue.ContainsKey(this.entity.entity))
            {
                Entity killer = ImprovedHordesManager.Instance.AIManager.entityKilledQueue[this.entity.entity];
                ImprovedHordesManager.Instance.AIManager.entityKilledQueue.Remove(this.entity.entity);

                if (killer is EntityAlive)
                    this.killer = killer as EntityAlive;
            }
        }
    }
}
