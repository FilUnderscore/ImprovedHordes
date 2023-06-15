 using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;
using System;

namespace ImprovedHordes.Implementations.World.Random
{
    public sealed class ImprovedHordesWorldRandomFactory : IRandomFactory<IWorldRandom>
    {
        private static readonly GameRandomManager GameRandomManager = GameRandomManager.Instance;

        private readonly int worldSize, worldSeed;
        private readonly IWorldRandom sharedRandom;

        public ImprovedHordesWorldRandomFactory(int worldSize, global::World world)
        {
            this.worldSize = worldSize;
            this.worldSeed = world.Seed;

            this.sharedRandom = new ImprovedHordesWorldRandom(world.GetGameRandom(), worldSize);
        }

        public IWorldRandom CreateRandom(int seed)
        {
            return new ImprovedHordesWorldRandom(GameRandomManager.CreateGameRandom(this.worldSeed + seed), this.worldSize);
        }

        public void FreeRandom(IWorldRandom random)
        {
            if (!(random is ImprovedHordesWorldRandom ihwr))
                throw new InvalidOperationException($"[Improved Hordes] Supplied IWorldRandom {random.GetType().Name} is not {nameof(ImprovedHordesWorldRandom)}");

            GameRandomManager.FreeGameRandom(ihwr.GetGameRandom());
        }

        public IWorldRandom GetSharedRandom()
        {
            return this.sharedRandom;
        }
    }
}
