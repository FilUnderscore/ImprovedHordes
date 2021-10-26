using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
