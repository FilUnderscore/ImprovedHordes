using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.World
{
    public class WorldEntitiesList
    {
        public static readonly Dictionary<WorldEntityType, List<WorldEntityDefinition>> WorldEntities = new Dictionary<WorldEntityType, List<WorldEntityDefinition>>();
    }
}
