using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.Horde
{
    public class HordeManager
    {
        private readonly List<Horde> hordes = new List<Horde>();

        public HordeManager()
        {
            // TODO: Rework hordes to all be per player, e.g. each player has their own wandering horde schedule that can merge if nearby players have similar occurances.
        }
    }
}
