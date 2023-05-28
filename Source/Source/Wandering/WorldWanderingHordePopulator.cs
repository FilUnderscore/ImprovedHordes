using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Horde.AI;
using ImprovedHordes.Source.Horde.AI.Commands;
using ImprovedHordes.Source.POI;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WorldWanderingHordePopulator : WorldZoneHordePopulator<WanderingHorde>
    {
        private const int MAX_NUMBER_OF_STOPS = 6;

        public WorldWanderingHordePopulator(WorldHordeTracker tracker, WorldHordeSpawner spawner, WorldPOIScanner scanner) : base(tracker, spawner, scanner)
        {
        }

        public override IEnumerable<AICommand> CreateHordeCommands(WorldPOIScanner.Zone zone)
        {
            yield return new GoToTargetAICommand(GetRandomZone().GetBounds().center);
        }

        private Vector3[] ConstructRandomPathToDestination(Vector3 initialPath, Vector3 destination)
        {
            return new Vector3[] { destination };

            Vector3 direction = (destination - initialPath).normalized;

            int stops = GameManager.Instance.World.GetGameRandom().RandomRange(MAX_NUMBER_OF_STOPS);

            for(int stop = 0; stop < stops; stop++)
            {
                float angle = Mathf.Atan2(direction.z, direction.x);

                float deviationAngleAllowance = Mathf.PI / 2;
            }
        }
    }
}
