using UnityEngine;

namespace ImprovedHordes.Core.World.Event
{
    public sealed class WorldEventReportEvent
    {
        private readonly Vector3 location;
        private readonly float interest;
        private readonly int distance;

        public WorldEventReportEvent(Vector3 location, float interest, int distance)
        {
            this.location = location;
            this.interest = interest;
            this.distance = distance;
        }

        public Vector3 GetLocation()
        {
            return this.location;
        }

        public float GetInterest() 
        {
            return this.interest; 
        }

        public int GetDistance() 
        {
            return this.distance;
        }
    }
}
