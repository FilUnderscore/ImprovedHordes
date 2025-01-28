using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.POI;
using ImprovedHordes.POI.Prefab;
using UnityEngine;

namespace ImprovedHordes.Wandering.Enemy.Zone
{
    public sealed class WorldZoneWanderingEnemyHordePopulator : WorldPrefabPOIZoneHordePopulator<WanderingEnemyHorde>
    {
        public WorldZoneWanderingEnemyHordePopulator(WorldPrefabPOIScanner scanner) : base(scanner)
        {
        }

        public override IAICommandGenerator<EntityAICommand> CreateEntityAICommandGenerator()
        {
            return null;
        }

        public override IAICommandGenerator<AICommand> CreateHordeAICommandGenerator(PrefabPOIZone zone)
        {
            return new WorldZoneWanderingEnemyAICommandGenerator(this.scanner, zone);
        }

        protected override int CalculateHordeCount(PrefabPOIZone zone)
        {
            return Mathf.FloorToInt(zone.GetBounds().size.magnitude / WorldHordeTracker.MAX_UNLOAD_VIEW_DISTANCE);
        }
    }
}
