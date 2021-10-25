﻿namespace ImprovedHordes.Horde.AI.Events
{
    public class HordeEntityKilledEvent
    {
        public readonly HordeAIEntity entity;
        
        public HordeEntityKilledEvent(HordeAIEntity entity)
        {
            this.entity = entity;
        }
    }
}
