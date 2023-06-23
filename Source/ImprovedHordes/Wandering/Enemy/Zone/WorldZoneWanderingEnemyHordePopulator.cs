using ImprovedHordes.Core.AI;
using ImprovedHordes.POI;
using UnityEngine;

namespace ImprovedHordes.Wandering.Enemy.Zone
{
    public sealed class WorldZoneWanderingEnemyHordePopulator : WorldZoneHordePopulator<WanderingEnemyHorde>
    {
        public WorldZoneWanderingEnemyHordePopulator(WorldPOIScanner scanner) : base(scanner)
        {
        }

        public override IAICommandGenerator<EntityAICommand> CreateEntityAICommandGenerator()
        {
            return null;
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(WorldPOIScanner.POIZone zone)
        {
            return new WorldZoneWanderingEnemyAICommandGenerator(this.scanner);
        }

        protected override int CalculateHordeCount(WorldPOIScanner.POIZone zone)
        {
            //return Mathf.FloorToInt(zone.GetCount() * zone.GetDensity());
            // TODO temporary until we can accurately measure zone size.

            return 1;
        }
    }
}
