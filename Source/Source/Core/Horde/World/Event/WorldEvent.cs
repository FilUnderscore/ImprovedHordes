using System;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public class WorldEvent
    {
        private const float TIME_SCALE = 1e5f;

        private Vector2i chunkLocation;
        private Vector3 blockLocation;

        private float interest;
        private float interest_ln;

        private readonly float strength;

        private double time;
        private double expire_time;

        public WorldEvent(Vector3 blockPosition, float interest) : this(blockPosition, global::World.toChunkXZ(blockPosition), interest, 1.0f) { }
        public WorldEvent(Vector3 blockPosition, float interest, float strength) : this(blockPosition, global::World.toChunkXZ(blockPosition), interest, strength) { }

        public WorldEvent(Vector3 blockPosition, Vector2i chunkLocation, float interest, float strength)
        {
            this.blockLocation = blockPosition;
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
            return this.blockLocation;
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

        public void Add(WorldEvent other)
        {
            float cap = 100.0f;
            if (other.strength != 1.0f)
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
