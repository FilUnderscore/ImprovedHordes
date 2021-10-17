using System.Collections.Generic;

namespace ImprovedHordes.Horde
{
    public class Horde
    {
        public readonly PlayerHordeGroup playerGroup;
        public readonly HordeGroup group;
        public readonly int count;
        public readonly bool feral;

        public readonly int[] entities;

        public Horde(PlayerHordeGroup playerGroup, HordeGroup group, int count, bool feral, int[] entities)
        {
            this.playerGroup = playerGroup;
            this.group = group;
            this.count = count;
            this.feral = feral;
            this.entities = entities;
        }

        public Horde(Horde horde) : this(horde.playerGroup, horde.group, horde.count, horde.feral, horde.entities) { }
    }
}
