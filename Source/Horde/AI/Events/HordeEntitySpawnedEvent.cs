using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.Horde.AI.Events
{
    public class HordeEntitySpawnedEvent
    {
        public readonly HordeAIEntity entity;
        public HordeAIHorde horde;

        public HordeEntitySpawnedEvent(HordeAIEntity entity, HordeAIHorde horde)
        {
            this.entity = entity;
            this.horde = horde;
        }
    }
}
