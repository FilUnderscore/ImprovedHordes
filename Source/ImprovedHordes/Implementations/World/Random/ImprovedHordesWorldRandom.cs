using ImprovedHordes.Core.Abstractions.World.Random;
using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Implementations.World.Random
{
    public sealed class ImprovedHordesWorldRandom : IWorldRandom
    {
        private readonly GameRandom random;
        private readonly int worldSize;

        public ImprovedHordesWorldRandom(GameRandom random, int worldSize)
        {
            this.random = random;
            this.worldSize = worldSize;
        }

        public Vector3 RandomLocation3
        {
            get
            {
                Vector2 location = RandomLocation2;
                float y = GameManager.Instance.World.GetHeightAt(location.x, location.y) + 1.0f;

                return new Vector3(location.x, y, location.y);
            }
        }

        public Vector2 RandomLocation2
        {
            get
            {
                Vector2 result = this.random.RandomInsideUnitCircle;
                return new Vector2(result.x * worldSize, result.y * worldSize);
            }
        }

        public T Random<T>(IList<T> collection)
        {
            int randomIndex = RandomRange(collection.Count);
            return collection[randomIndex];
        }

        public int RandomRange(int maxExclusive)
        {
            return this.random.RandomRange(maxExclusive);
        }

        public GameRandom GetGameRandom()
        {
            return this.random;
        }

        public bool RandomChance(float pct)
        {
            return this.random.RandomFloat <= pct;
        }
    }
}
