﻿using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.POI;

namespace ImprovedHordes.Wandering.Enemy.Zone
{
    public sealed class WorldZoneWanderingEnemyHordePopulator : WorldZoneHordePopulator<WanderingEnemyHorde>
    {
        public WorldZoneWanderingEnemyHordePopulator(WorldHordeTracker tracker, WorldPOIScanner scanner) : base(tracker, scanner)
        {
        }

        public override IAICommandGenerator<EntityAICommand> CreateEntityAICommandGenerator()
        {
            return null;
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(WorldPOIScanner.Zone zone)
        {
            return new WorldZoneWanderingEnemyAICommandGenerator(this.scanner);
        }

        protected override int CalculateHordeCount(WorldPOIScanner.Zone zone)
        {
            //int maxRadius = Mathf.RoundToInt(zone.GetBounds().size.magnitude / 4);
            //return Mathf.CeilToInt(((float)maxRadius / zone.GetCount()) * zone.GetDensity());
            return zone.GetCount();
        }
    }
}
