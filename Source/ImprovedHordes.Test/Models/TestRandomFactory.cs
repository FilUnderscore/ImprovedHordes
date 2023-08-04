using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;

namespace ImprovedHordes.Test.Models
{
    public sealed class TestRandomFactory : IRandomFactory<IWorldRandom>
    {
        private readonly TestWorldRandom sharedRandom = new TestWorldRandom();

        IWorldRandom IRandomFactory<IWorldRandom>.CreateRandom(int seed)
        {
            return new TestWorldRandom();
        }

        public void FreeRandom(IWorldRandom random)
        {
        }

        IWorldRandom IRandomFactory<IWorldRandom>.GetSharedRandom()
        {
            return sharedRandom;
        }
    }
}
