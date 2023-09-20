using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Horde.AI.Commands;
using ImprovedHordes.POI;
using UnityEngine;

namespace ImprovedHordes.Screamer.Commands
{
    public sealed class ZoneWanderAICommand : GoToTargetAICommand
    {
        private readonly WorldPOIScanner.POIZone zone;
        private readonly IWorldRandom random;
        private readonly bool useWanderCount;

        private int wanderCount;

        public ZoneWanderAICommand(WorldPOIScanner.POIZone zone, IWorldRandom random, bool useWanderCount) : base(GetNextTarget(zone, random, out int wanderCount, out float wanderTicks), false, false)
        {
            this.zone = zone;
            this.random = random;
            this.useWanderCount = useWanderCount;

            float wanderChance = zone.GetDensity();

            if (random.RandomFloat <= wanderChance) // Random chance for the horde to wander around the zone depending on the density.
            {
                this.wanderCount = wanderCount;
            }
            else
            {
                this.wanderCount = 0;
            }
        }

        private static Vector3 GetNextTarget(WorldPOIScanner.POIZone zone, IWorldRandom random, out int wanderCount, out float wanderTicks)
        {
            zone.GetLocationOutside(random, out Vector2 targetPos2);
            float y = GameManager.Instance.World.GetHeightAt(targetPos2.x, targetPos2.y) + 1.0f;

            float zoneSize = zone.GetBounds().size.magnitude / 2.0f;

            wanderCount = Mathf.CeilToInt(Mathf.Sqrt(zone.GetCount()));
            wanderTicks = (1.0f + random.RandomFloat) * (zoneSize / zone.GetDensity()) / zone.GetCount();

            return new Vector3(targetPos2.x, y, targetPos2.y);
        }

        public override void Execute(IAIAgent agent, float dt)
        {
            base.Execute(agent, dt);
        }

        public override bool IsComplete(IAIAgent agent)
        {
            if(this.useWanderCount && this.wanderCount <= 0)
                return base.IsComplete(agent);

            if(base.IsComplete(agent))
            {
                if (this.useWanderCount)
                    this.wanderCount--;

                agent.Stop();
                this.UpdateTarget(GetNextTarget(this.zone, this.random, out _, out _));
            }

            return false;
        }
    }
}
