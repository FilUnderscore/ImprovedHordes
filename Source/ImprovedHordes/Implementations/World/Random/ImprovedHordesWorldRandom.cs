﻿using ImprovedHordes.Core.Abstractions.World.Random;
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
                float randomX = RandomFloat * worldSize - worldSize / 2.0f;
                float randomY = RandomFloat * worldSize - worldSize / 2.0f;

                return new Vector2(randomX, randomY);
            }
        }

        public T Random<T>(IList<T> collection)
        {
            if (collection == null || collection.Count == 0)
                return default(T);

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

        public float RandomFloat
        {
            get
            {
                return this.random.RandomFloat;
            }
        }

        public bool RandomChance(float pct)
        {
            return RandomFloat <= pct;
        }

        public Vector2 RandomOnUnitCircle
        {
            get
            {
                return this.random.RandomOnUnitCircle;
            }
        }

        public Vector2 RandomInsideUnitCircle
        {
            get
            {
                return this.random.RandomInsideUnitCircle;
            }
        }
    }
}
