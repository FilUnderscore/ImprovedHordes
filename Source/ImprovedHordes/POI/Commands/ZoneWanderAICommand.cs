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

        private float wanderTicks = 0.0f;

        public ZoneWanderAICommand(WorldPOIScanner.POIZone zone, IWorldRandom random, bool useWanderCount) : base(GetNextTarget(zone, random, out int wanderCount, out float wanderTicks))
        {
            this.zone = zone;
            this.random = random;
            this.useWanderCount = useWanderCount;

            this.wanderCount = wanderCount;
            this.wanderTicks = wanderTicks;
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
            if ((this.wanderTicks -= dt) > 0.0f)
            {
                if(agent.IsMoving())
                    agent.Stop();
    
                return;
            }

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
                
                this.UpdateTarget(GetNextTarget(this.zone, this.random, out this.wanderCount, out this.wanderTicks));
            }

            return false;
        }
    }
}
