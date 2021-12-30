using System.Collections.Generic;

using ImprovedHordes.Horde.Data;

namespace ImprovedHordes.Horde
{
    public class Horde
    {
        public readonly PlayerHordeGroup playerGroup;
        public readonly HordeGroup group;
        public readonly int count;
        public readonly bool feral;

        public readonly List<int> entityIds;

        public Horde(PlayerHordeGroup playerGroup, HordeGroup group, int count, bool feral, List<int> entityIds)
        {
            this.playerGroup = playerGroup;
            this.group = group;
            this.count = count;
            this.feral = feral;
            this.entityIds = entityIds;
        }

        public Horde(Horde horde) : this(horde.playerGroup, horde.group, horde.count, horde.feral, horde.entityIds) { }
    }
}
