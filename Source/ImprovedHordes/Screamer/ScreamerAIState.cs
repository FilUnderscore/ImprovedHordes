using ImprovedHordes.Core.AI;
using ImprovedHordes.POI;

namespace ImprovedHordes.Screamer
{
    public sealed class ScreamerAIState : IAIState
    {
        private readonly WorldPOIScanner.POIZone zone;

        public ScreamerAIState(WorldPOIScanner.POIZone zone)
        {
            this.zone = zone;
        }

        public WorldPOIScanner.POIZone GetPOIZone()
        {
            return this.zone;
        }
    }
}
