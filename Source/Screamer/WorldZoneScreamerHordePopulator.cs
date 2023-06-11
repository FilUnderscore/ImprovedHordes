using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Event;
using ImprovedHordes.POI;

namespace ImprovedHordes.Screamer
{
    public sealed class WorldZoneScreamerHordePopulator : WorldZoneHordePopulator<ScreamerHorde>
    {
        private readonly WorldEventReporter worldEventReporter;

        public WorldZoneScreamerHordePopulator(WorldPOIScanner scanner, WorldEventReporter worldEventReporter) : base(scanner)
        {
            this.worldEventReporter = worldEventReporter;
        }

        public override IAICommandGenerator<EntityAICommand> CreateEntityAICommandGenerator()
        {
            return new ScreamerEntityAICommandGenerator(this.worldEventReporter);
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(WorldPOIScanner.Zone zone)
        {
            return new GoToWorldZoneAICommandGenerator(this.scanner);
        }

        protected override int CalculateHordeCount(WorldPOIScanner.Zone zone)
        {
            return 1;
        }
    }
}