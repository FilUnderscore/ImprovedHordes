using ImprovedHordes.Source.Horde.AI;
using ImprovedHordes.Source.Horde.AI.Commands;
using ImprovedHordes.Source.POI;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Scout
{
    public class WorldZoneScoutHordePopulator : WorldZoneHordePopulator<ScoutHorde>
    {
        public WorldZoneScoutHordePopulator(WorldPOIScanner scanner) : base(scanner)
        {
        }

        public override IEnumerable<AICommand> CreateHordeCommands(WorldPOIScanner.Zone zone)
        {
            yield return new GoToTargetAICommand(GetRandomZone().GetBounds().center);
        }

        protected override int CalculateHordeCount(WorldPOIScanner.Zone zone)
        {
            return 1;
        }
    }
}