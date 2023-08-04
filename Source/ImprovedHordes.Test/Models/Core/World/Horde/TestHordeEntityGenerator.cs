using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.World.Horde;
using UnityEngine;

namespace ImprovedHordes.Test.Models.Core.World.Horde
{
    public sealed class TestHordeEntityGenerator : HordeEntityGenerator
    {
        public TestHordeEntityGenerator(PlayerHordeGroup playerGroup) : base(playerGroup)
        {
        }

        public override int DetermineEntityCount(float density)
        {
            return Mathf.CeilToInt(density);
        }

        public override int GetEntityClassId(IRandom random)
        {
            return 0;
        }

        public override bool IsStillValidFor(PlayerHordeGroup playerGroup)
        {
            return true;
        }
    }
}
