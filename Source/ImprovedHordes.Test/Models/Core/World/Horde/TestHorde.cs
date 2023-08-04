using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Characteristics;

namespace ImprovedHordes.Test.Models.Core.World.Horde
{
    public sealed class TestHorde : IHorde
    {
        public bool CanMergeWith(IHorde other)
        {
            return true;
        }

        public HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.0f, 2.0f), new SensitivityHordeCharacteristic(1.0f));
        }

        public HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup, IRandom random)
        {
            return new TestHordeEntityGenerator(playerGroup);
        }

        public HordeType GetHordeType()
        {
            return HordeType.ENEMY;
        }
    }
}
