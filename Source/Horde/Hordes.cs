using System.Collections.Generic;

namespace ImprovedHordes.Horde
{
    public class Hordes
    {
        public static Dictionary<string, Dictionary<string, HordeGroup>> hordes = new Dictionary<string, Dictionary<string, HordeGroup>>();
    
        public static Dictionary<string, HordeGroup> GetHordeGroupsForHorde(string horde)
        {
            return hordes[horde];
        }

        public static HordeGroup GetHordeGroupByName(string horde, string name)
        {
            return hordes[horde][name];
        }
    }
}
