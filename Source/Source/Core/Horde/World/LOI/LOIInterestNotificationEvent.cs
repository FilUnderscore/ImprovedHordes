using System;
using UnityEngine;

namespace ImprovedHordes.Source.Horde.World.LOI
{
    public sealed class LOIInterestNotificationEvent : EventArgs
    {
        private readonly Vector3 locationOfInterest;
        private readonly float interestLevel;
        private readonly int distance;

        public LOIInterestNotificationEvent(Vector3 locationOfInterest, float interestLevel, int distance)
        {
            this.locationOfInterest = locationOfInterest;
            this.interestLevel = interestLevel;
            this.distance = distance;
        }

        public Vector3 GetLocation()
        {
            return this.locationOfInterest;
        }

        public float GetInterestLevel() 
        {
            return this.interestLevel;
        }

        public int GetDistance()
        {
            return this.distance;
        }
    }
}