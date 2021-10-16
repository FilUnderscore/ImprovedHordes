using System;
using UnityEngine;

namespace ImprovedHordes.Horde.AI.Commands
{
    public class HordeAICommandDestinationMoving : HordeAICommandDestination
    {
        private const float TICKS_PER_UPDATE = 10f;

        private readonly Func<Vector3> destinationFunction;

        private float ticksToUpdate;

        public HordeAICommandDestinationMoving(Func<Vector3> destinationFunction, int distanceTolerance) : base(destinationFunction.Invoke(), distanceTolerance)
        {
            this.destinationFunction = destinationFunction;
        }

        public override void Execute(float dt, EntityAlive alive)
        {
            base.Execute(dt, alive);

            if (ticksToUpdate <= 0.0f)
            {
                this.targetPosition = this.destinationFunction.Invoke();
                this.ticksToUpdate = TICKS_PER_UPDATE;
            }
            else
                ticksToUpdate -= dt;
        }
    }
}
