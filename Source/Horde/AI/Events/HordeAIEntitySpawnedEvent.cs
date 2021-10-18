using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.Horde.AI.Events
{
    public class HordeAIEntitySpawnedEvent
    {
        public readonly HordeAIEntity entity;
        public HordeAIHorde horde;

        public HordeAIEntitySpawnedEvent(HordeAIEntity entity, HordeAIHorde horde)
        {
            this.entity = entity;
            this.horde = horde;
        }
    }
}
