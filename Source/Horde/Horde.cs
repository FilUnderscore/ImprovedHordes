using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ImprovedHordes.IHLog;

namespace ImprovedHordes.Horde
{
    public abstract class Horde
    {
        public HordeGroup group;
        public int count;
        public bool feral;

        public int[] entities;

        public Horde(HordeGroup group, int count, bool feral, int[] entities)
        {
            this.group = group;
            this.count = count;
            this.feral = feral;
            this.entities = entities;
        }

        public Horde(Horde horde) : this(horde.group, horde.count, horde.feral, horde.entities) { }
    }
}
