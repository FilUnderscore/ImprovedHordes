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
    public struct PlayerHordeGroup
    {
        public EntityPlayer leader;
        public List<EntityPlayer> members;

        public PlayerHordeGroup(EntityPlayer leader, List<EntityPlayer> members)
        {
            this.leader = leader;
            this.members = members;

            if (this.members != null && this.members.Contains(this.leader))
                this.members.Remove(this.leader);
        }

        public List<EntityPlayer> GetAllPlayers()
        {
            List<EntityPlayer> copy = new List<EntityPlayer>(members);
            copy.Insert(0, this.leader);

            return copy;
        }

        public int GetGroupGamestage()
        {
            List<int> gamestages = new List<int>();
            List<EntityPlayer> allPlayers = GetAllPlayers();

            foreach(var player in allPlayers)
            {
                gamestages.Add(player.gameStage);
            }

            return GameStageDefinition.CalcPartyLevel(gamestages);
        }
    }
}
