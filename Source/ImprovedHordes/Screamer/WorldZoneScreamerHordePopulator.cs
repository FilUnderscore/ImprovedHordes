using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Event;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.POI;

namespace ImprovedHordes.Screamer
{
    public sealed class WorldZoneScreamerHordePopulator : WorldZoneHordePopulator<ScreamerHorde>
    {
        private readonly WorldEventReporter worldEventReporter;

        public WorldZoneScreamerHordePopulator(WorldHordeTracker tracker, WorldPOIScanner scanner, WorldEventReporter worldEventReporter) : base(tracker, scanner)
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

        protected override float GetMinimumDensity()
        {
            return 0.2f;
        }
    }
}