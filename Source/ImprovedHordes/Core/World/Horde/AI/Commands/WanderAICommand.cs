using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.AI;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde.AI.Commands
{
    public sealed class WanderAICommand : GoToTargetAICommand
    {
        private readonly Vector2 pos;
        private readonly IWorldRandom random;
        private readonly float wanderRadius;
        private float wanderTime;

        public WanderAICommand(Vector2 pos, IWorldRandom random, float wanderRadius, float wanderTime) : base(GetNextTarget(pos, random, wanderRadius))
        {
            this.pos = pos;
            this.random = random;
            this.wanderRadius = wanderRadius;
            this.wanderTime = wanderTime;
        }

        public WanderAICommand(Vector3 pos, IWorldRandom random, float wanderRadius, float wanderTime) : this(new Vector2(pos.x, pos.z), random, wanderRadius, wanderTime)
        {
        }

        private static Vector3 GetNextTarget(Vector2 pos, IWorldRandom random, float wanderRadius)
        {
            Vector2 posInsideCircle = pos + random.RandomInsideUnitCircle * wanderRadius;
            float y = GameManager.Instance.World.GetHeightAt(posInsideCircle.x, posInsideCircle.y) + 1.0f;

            return new Vector3(posInsideCircle.x, y, posInsideCircle.y);
        }

        public override void Execute(IAIAgent agent, float dt)
        {
            this.wanderTime -= dt;
            base.Execute(agent, dt);
        }

        public override bool IsComplete(IAIAgent agent)
        {
            if(base.IsComplete(agent))
            {
                agent.Stop();
                this.UpdateTarget(GetNextTarget(this.pos, this.random, this.wanderRadius));
            }

            return this.wanderTime <= 0.0f;
        }

        public float GetWanderTime()
        {
            return this.wanderTime;
        }
    }
}
