using System;
using UnityEngine;

namespace ImprovedHordes.Core.World.Event
{
    public sealed class WorldEvent
    {
        private const float TIME_SCALE = 1e1f;

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

            this.SetInterest(interest);

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

        public float GetInterestLevel()
        {
            //L=-((ln(a+1))/c)t^2+a
            //(c*(L-a) / -(ln(a+1)))=t^2

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

        private void SetInterest(float interest)
        {
            this.interest = interest;
            this.interest_ln = Mathf.Log(this.interest + 1);
        }

        public void Add(WorldEvent other)
        {
            float cap = 100.0f;
            if (other.strength < 1.0f)
            {
                cap *= other.strength;
            }

            this.SetInterest(Mathf.Clamp(this.interest + other.interest, 0.0f, cap));

            double timeDiff = (other.time - this.time) / 2;

            this.time = other.time - (timeDiff * ((100.0f - this.interest) / 100.0f));
            this.expire_time = GetExpireTime();
        }
    }
}
