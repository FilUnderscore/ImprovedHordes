using System.Collections.Generic;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Data
{
    public sealed class HordeGroupList
    {
        public readonly string type;
        public readonly Dictionary<string, HordeGroup> hordes = new Dictionary<string, HordeGroup>();

        public HordeGroupList(string type)
        {
            this.type = type;
        }

        public void SortParentsAndChildrenOut()
        {
            Dictionary<string, List<HordeGroup>> parentsAndChildren = new Dictionary<string, List<HordeGroup>>();

            foreach(var group in hordes.Values)
            {
                if (group.parent == null)
                    continue;

                if(!hordes.ContainsKey(group.parent))
                {
                    Warning("Horde group {0} has undefined parent {1} in type {2}. May result in unintended behavior.", group.name, group.parent, this.type);
                    continue;
                }

                if (!parentsAndChildren.ContainsKey(group.parent))
                    parentsAndChildren.Add(group.parent, new List<HordeGroup>());

                parentsAndChildren[group.parent].Add(group);
            }

            foreach(var parentEntry in parentsAndChildren)
            {
                var parentGroup = hordes[parentEntry.Key];
                parentGroup.children = parentEntry.Value;

                Log("Parent Group {0} Children {1}", parentGroup.name, parentGroup.children.ToString(child => child.name));
            }
        }
    }
}
