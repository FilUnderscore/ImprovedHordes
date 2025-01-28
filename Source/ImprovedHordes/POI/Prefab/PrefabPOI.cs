using ImprovedHordes.Core.Abstractions.World.Random;
using UnityEngine;

namespace ImprovedHordes.POI.Prefab
{
    public sealed class PrefabPOI : POI
    {
        private PrefabInstance prefab;
        private float weight;

        private PrefabPOI closestPOI;

        public PrefabPOI(PrefabInstance prefab)
        {
            this.prefab = prefab;
            this.weight = 0.0f;
        }

        public void MarkZoned(PrefabPOI other)
        {
            this.weight += 1.0f;

            if (this.closestPOI == null || Vector2.Distance(this.GetLocation(), other.GetLocation()) < Vector2.Distance(closestPOI.GetLocation(), other.GetLocation()))
                this.closestPOI = other;
        }

        public float GetWeight()
        {
            return this.weight;
        }

        public Vector2 GetLocation()
        {
            return this.prefab.GetCenterXZ();
        }

        public Bounds GetBounds()
        {
            return this.prefab.GetAABB();
        }

        public void GetLocationOutside(IWorldRandom worldRandom, out Vector2 location)
        {
            float minRange = this.GetBounds().size.magnitude / 2;

            if (this.closestPOI == null)
            {
                location = this.GetLocation() + worldRandom.RandomOnUnitCircle * minRange;
                return;
            }

            float closestPOIDistance = Vector2.Distance(this.GetLocation(), this.closestPOI.GetLocation());
            float closestPOIMinRange = this.closestPOI.GetBounds().size.magnitude / 2;

            float maxRange = closestPOIDistance - closestPOIMinRange;

            float range = worldRandom.RandomFloat * (maxRange - minRange) + minRange;
            location = this.GetLocation() + worldRandom.RandomOnUnitCircle * range;
        }

        public bool IsPlayerConvertedPOI()
        {
            Vector3i position = new Vector3i(this.GetBounds().center);
            return GameManager.Instance.World.GetLandClaimOwner(position, GameManager.Instance.GetPersistentLocalPlayer()) != EnumLandClaimOwner.None || GameManager.Instance.World.IsWithinTraderArea(position);
        }
    }
}
