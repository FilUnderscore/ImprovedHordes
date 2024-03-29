﻿using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde;
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
            return new WorldZoneWanderingEnemyAICommandGenerator(this.scanner, zone);
        }

        protected override int CalculateHordeCount(WorldPOIScanner.POIZone zone)
        {
            return Mathf.FloorToInt(zone.GetBounds().size.magnitude / WorldHordeTracker.MAX_UNLOAD_VIEW_DISTANCE);
        }
    }
}
