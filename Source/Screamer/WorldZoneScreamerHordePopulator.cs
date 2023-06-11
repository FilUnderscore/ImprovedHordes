using ImprovedHordes.Core.AI;
using ImprovedHordes.POI;

namespace ImprovedHordes.Screamer
{
    public class WorldZoneScreamerHordePopulator : WorldZoneHordePopulator<ScreamerHorde>
    {
        public WorldZoneScreamerHordePopulator(WorldPOIScanner scanner) : base(scanner)
        {
        }

        public override IAICommandGenerator CreateHordeAICommandGenerator(WorldPOIScanner.Zone zone)
        {
            return new GoToWorldZoneAICommandGenerator(this.scanner);
        }

        protected override int CalculateHordeCount(WorldPOIScanner.Zone zone)
        {
            return 1;
        }
    }
}