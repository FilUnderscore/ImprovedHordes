using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Horde
{
    public class PlayerHordeGroup
    {
        public HashSet<EntityPlayer> members;

        public PlayerHordeGroup(HashSet<EntityPlayer> members)
        {
            this.members = members;
        }

        public PlayerHordeGroup(params EntityPlayer[] players)
        {
            this.members = new HashSet<EntityPlayer>(players);
        }

        public int GetGroupGamestage(Vector3 pos)
        {
            List<int> gamestages = new List<int>();
            
            foreach (var player in this.members)
            {
                gamestages.Add(player.gameStage);
            }

            int groupGSDiff = gamestages.Max() - gamestages.Min();
            int groupGS = (gamestages.Sum() / Mathf.Max(1, gamestages.Count - 1)) - groupGSDiff;

            float heatDiff = 0.25f * (ImprovedHordesManager.Instance.HeatTracker.GetHeatForGroup(this) / 100f);
            BiomeDefinition biomeDef = ImprovedHordesManager.Instance.World.GetBiome((int)pos.x, (int)pos.z);
            float biomeDiff = biomeDef.LootStageMod;
            float biomeBonus = biomeDef.LootStageBonus;

            return global::Utils.Fastfloor(groupGS * (1f + heatDiff) + biomeBonus * biomeDiff);
        }

        public int GetGroupGamestage()
        {
            return GetGroupGamestage(CalculateAverageGroupPosition(false));
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

        public override bool Equals(object obj)
        {
            if (!(obj is PlayerHordeGroup))
                return false;

            var other = obj as PlayerHordeGroup;

            return this.members.SetEquals(other.members);
        }

        public override int GetHashCode()
        {
            var comparer = HashSet<EntityPlayer>.CreateSetComparer();
            
            return comparer.GetHashCode(this.members);
        }
    }
}
