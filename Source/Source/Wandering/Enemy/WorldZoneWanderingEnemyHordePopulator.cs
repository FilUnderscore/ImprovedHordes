using ImprovedHordes.Source.Core.Horde.World.Cluster.AI;
using ImprovedHordes.Source.POI;
using UnityEngine;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WorldZoneWanderingEnemyHordePopulator : WorldZoneHordePopulator<WanderingEnemyHorde>
    {
        private const int MAX_NUMBER_OF_STOPS = 6;

        public WorldZoneWanderingEnemyHordePopulator(WorldPOIScanner scanner) : base(scanner)
        {
        }

        public override IAICommandGenerator CreateHordeAICommandGenerator(WorldPOIScanner.Zone zone)
        {
            return new WorldZoneWanderingEnemyAICommandGenerator(this.scanner);
        }

        protected override int CalculateHordeCount(WorldPOIScanner.Zone zone)
        {
            int maxRadius = Mathf.RoundToInt(zone.GetBounds().size.magnitude / 2);
            return Mathf.CeilToInt(((float)maxRadius / zone.GetCount()) * zone.GetDensity());
        }
    }
}
