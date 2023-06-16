using ImprovedHordes.Core.World.Horde.AI.Commands;
using ImprovedHordes.POI;
using UnityEngine;

namespace ImprovedHordes.Screamer.Commands
{
    public sealed class ZoneWanderAICommand : GoToTargetAICommand
    {
        public ZoneWanderAICommand(WorldPOIScanner.POIZone zone) : base(GetTarget(zone))
        {
        }

        private static Vector3 GetTarget(WorldPOIScanner.POIZone zone)
        {
            Vector2 targetPos2 = zone.GetCenter() + zone.GetBounds().size.magnitude * 2 * GameManager.Instance.World.GetGameRandom().RandomOnUnitCircle;
            float y = GameManager.Instance.World.GetHeightAt(targetPos2.x, targetPos2.y);

            return new Vector3(targetPos2.x, y, targetPos2.y);
        }
    }
}
