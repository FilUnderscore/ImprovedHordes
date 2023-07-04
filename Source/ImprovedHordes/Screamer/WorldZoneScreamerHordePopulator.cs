using HarmonyLib;
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

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(WorldPOIScanner.POIZone zone)
        {
            return new WorldZoneScreamerAICommandGenerator(zone);
        }

        protected override int CalculateHordeCount(WorldPOIScanner.POIZone zone)
        {
            return 1;
        }

        protected override bool IsDensityInfluencedByZoneProperties()
        {
            return false;
        }

        protected override float GetMinimumDensity()
        {
            return this.scanner.GetAverageZoneDensity() / 2;
        }

        [HarmonyPatch(typeof(AIDirectorChunkEventComponent))]
        [HarmonyPatch("SpawnScouts")]
        class AIDirectorChunkEventComponent_SpawnScouts_Patch
        {
            static bool Prefix()
            {
                // Prevent default scout horde from spawning.
                return false;
            }
        }
    }
}