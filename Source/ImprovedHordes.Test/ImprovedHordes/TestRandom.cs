using ImprovedHordes.Core.Abstractions.World.Random;
using UnityEngine;

namespace ImprovedHordes.Test
{
    public sealed class TestWorldRandom : IWorldRandom
    {
        public Vector3 RandomLocation3 => Vector3.zero;

        public Vector2 RandomLocation2 => Vector2.zero;

        public float RandomFloat => 0.0f;

        public Vector2 RandomOnUnitCircle => Vector2.zero;

        public T Random<T>(IList<T> collection)
        {
            return collection[0];
        }

        public bool RandomChance(float pct)
        {
            return true;
        }

        public int RandomRange(int maxExclusive)
        {
            return 0;
        }
    }
}
