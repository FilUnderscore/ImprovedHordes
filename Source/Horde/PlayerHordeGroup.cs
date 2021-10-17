using System.Collections.Generic;

namespace ImprovedHordes.Horde
{
    public class PlayerHordeGroup
    {
        public List<EntityPlayer> members;

        public PlayerHordeGroup(List<EntityPlayer> members)
        {
            this.members = members;
        }

        public int GetGroupGamestage()
        {
            List<int> gamestages = new List<int>();
            
            foreach (var player in this.members)
            {
                gamestages.Add(player.gameStage);
            }

            return GameStageDefinition.CalcPartyLevel(gamestages);
        }

        public override string ToString()
        {
            return $"PlayerHordeGroup [members={members.ToString(player => player.EntityName)}]";
        }
    }
}
