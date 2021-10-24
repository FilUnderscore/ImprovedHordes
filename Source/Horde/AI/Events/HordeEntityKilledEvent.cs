using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.Horde.AI.Events
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
