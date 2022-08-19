using System.Collections.Generic;

using ImprovedHordes.Horde.Data;

namespace ImprovedHordes.Horde
{
    public class Horde
    {
        public readonly PlayerHordeGroup playerGroup;
        public readonly HordeGroup group;
        public readonly int gamestage;
        public readonly int count;
        public readonly bool feral;

        public readonly List<int> entityIds;

        public Horde(PlayerHordeGroup playerGroup, HordeGroup group, int gamestage, int count, bool feral, List<int> entityIds)
        {
            this.playerGroup = playerGroup;
            this.group = group;
            this.gamestage = gamestage;
            this.count = count;
            this.feral = feral;
            this.entityIds = entityIds;
        }

        public Horde(Horde horde) : this(horde.playerGroup, horde.group, horde.gamestage, horde.count, horde.feral, horde.entityIds) { }

        public override string ToString()
        {
            return $"Horde [group={group.name}, count={count}, feral={feral}, gamestage={gamestage}, entityIds={entityIds.ToString(entityId => entityId.ToString())}]";
        }
    }
}
