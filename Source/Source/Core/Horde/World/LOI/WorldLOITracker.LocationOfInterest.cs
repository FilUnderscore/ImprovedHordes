using System;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.LOI
{
    public sealed partial class WorldLOITracker
    {
        private class LocationOfInterest
        {
            private const float TIME_SCALE = 1e5f;

            private Vector2i chunkLocation;
            private float interest;
            private float interest_ln;

            private readonly float strength;

            private double time;
            private double expire_time;
            
            public LocationOfInterest(Vector3 position, float interest) : this(global::World.toChunkXZ(position), interest, 1.0f) { }
            public LocationOfInterest(Vector3 position, float interest, float strength) : this(global::World.toChunkXZ(position), interest, strength) { }

            public LocationOfInterest(Vector2i chunkLocation, float interest, float strength)
            {
                this.chunkLocation = chunkLocation;
                this.interest = interest;
                this.interest_ln = Mathf.Log(interest + 1);

                this.strength = strength;

                this.time = Time.timeAsDouble;
                this.expire_time = GetExpireTime();
            }

            public Vector2i GetChunkLocation()
            {
                return this.chunkLocation;
            }

            public Vector3 GetLocation()
            {
                float centerX = this.chunkLocation.x * 16 - 8;
                float centerZ = this.chunkLocation.y * 16 - 8;
                float centerY = GameManager.Instance.World.GetHeightAt(centerX, centerZ) + 1.0f;

                return new Vector3(centerX, centerY, centerZ);
            }

            public float GetInterestLevel() // TODO: Scale interest near blood moons.
            {
                //-((ln(a+1))/c)t^2+a

                float slope = -(interest_ln / TIME_SCALE);
                float offset = interest;

                double timeSince = Time.timeAsDouble - time;
                double timeSinceSquared = timeSince * timeSince;
                float timeSinceSquaredFloat = (float)timeSinceSquared;

                float decayingInterest = slope * timeSinceSquaredFloat + offset;

                return decayingInterest;
            }

            private double GetExpireTime()
            {
                double topSqrt = TIME_SCALE * this.interest;
                double bottomSqrt = this.interest_ln;

                return Math.Sqrt(topSqrt / bottomSqrt);
            }

            public bool HasLostInterest()
            {
                return (Time.timeAsDouble - time) > this.expire_time;
            }

            public float GetStrength()
            {
                return this.strength;
            }

            public void Add(LocationOfInterest other)
            {
                float cap = 100.0f;
                if(other.strength != 1.0f)
                {
                    cap *= other.strength;
                }

                this.interest = Mathf.Clamp(this.interest + other.interest, 0.0f, cap);
                this.interest_ln = Mathf.Log(this.interest + 1);

                this.time = Time.timeAsDouble;
                this.expire_time = GetExpireTime();
            }
        }
    }
}
