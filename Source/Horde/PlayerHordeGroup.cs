using System.Collections.Generic;

using UnityEngine;

namespace ImprovedHordes.Horde
{
    public class PlayerHordeGroup
    {
        public List<EntityPlayer> members;

        public PlayerHordeGroup(List<EntityPlayer> members)
        {
            this.members = members;
        }

        public PlayerHordeGroup(params EntityPlayer[] players)
        {
            this.members = new List<EntityPlayer>(players);
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

        public Vector3 CalculateAverageGroupPosition(bool calculateY)
        {
            Vector3 avg = Vector3.zero;

            foreach (var player in this.members)
            {
                avg += player.position;
            }

            avg /= this.members.Count;

            if(calculateY)
                Utils.GetSpawnableY(ref avg);

            return avg;
        }

        public override string ToString()
        {
            return $"PlayerHordeGroup [members={members.ToString(player => player.EntityName)}]";
        }
    }
}
