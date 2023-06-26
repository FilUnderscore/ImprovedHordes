﻿using ImprovedHordes.Core.AI;
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
            return Mathf.Max(1, Mathf.CeilToInt((zone.GetBounds().size.magnitude / (2 * zone.GetCount())) * zone.GetDensity()));
        }
    }
}
