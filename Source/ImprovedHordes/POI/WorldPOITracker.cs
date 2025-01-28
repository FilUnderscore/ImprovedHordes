using ImprovedHordes.Core.Abstractions.World.Random;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImprovedHordes.POI
{
    public sealed class WorldPOITracker
    {
        private readonly List<POI> pois = new List<POI>();

        public void Track(POI poi)
        {
            this.pois.Add(poi);
        }

        public POI GetRandomPOI(IWorldRandom worldRandom, Func<POI, bool> predicate)
        {
            return worldRandom.Random(pois.Where(predicate));
        }
    }
}
