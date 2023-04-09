using System;
using UnityEngine;

namespace ImprovedHordes.Source.Horde.World.LOI
{
    public sealed class LOIInterestNotificationEvent : EventArgs
    {
        private readonly Vector3 locationOfInterest;
        private readonly int distance;

        public LOIInterestNotificationEvent(Vector3 locationOfInterest, int distance)
        {
            this.locationOfInterest = locationOfInterest;
            this.distance = distance;
        }

        public Vector3 GetLocation()
        {
            return this.locationOfInterest;
        }

        public int GetDistance()
        {
            return this.distance;
        }
    }
}