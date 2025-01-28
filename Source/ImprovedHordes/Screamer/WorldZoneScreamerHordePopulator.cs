using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Event;
using ImprovedHordes.POI;
using ImprovedHordes.POI.Prefab;

namespace ImprovedHordes.Screamer
{
    public sealed class WorldZoneScreamerHordePopulator : WorldPrefabPOIZoneHordePopulator<ScreamerHorde>
    {
        private readonly WorldEventReporter worldEventReporter;

        public WorldZoneScreamerHordePopulator(WorldPrefabPOIScanner scanner, WorldEventReporter worldEventReporter) : base(scanner)
        {
            this.worldEventReporter = worldEventReporter;
        }

        public override IAICommandGenerator<EntityAICommand> CreateEntityAICommandGenerator()
        {
            return new ScreamerEntityAICommandGenerator(this.worldEventReporter);
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(PrefabPOIZone zone)
        {
            return new WorldZoneScreamerAICommandGenerator(zone);
        }

        protected override int CalculateHordeCount(PrefabPOIZone zone)
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

        /*
        [HarmonyPatch(typeof(AIDirectorChunkEventComponent))]
        [HarmonyPatch(nameof(AIDirectorChunkEventComponent.SpawnScouts))]
        class AIDirectorChunkEventComponent_SpawnScouts_Patch
        {
            static bool Prefix()
            {
                // Prevent default scout horde from spawning.
                return false;
            }
        }
        */
    }
}