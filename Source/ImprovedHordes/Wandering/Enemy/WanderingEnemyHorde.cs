using ImprovedHordes.Data;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Core.World.Horde;

namespace ImprovedHordes.Wandering.Enemy
{
    public sealed class WanderingEnemyHorde : HordeDefinitionHorde
    {
        public WanderingEnemyHorde() : base("wandering_enemy")
        {

        }

        public override HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.6f, 3.2f), new SensitivityHordeCharacteristic(1.0f));
        }

        public override HordeType GetHordeType()
        {
            return HordeType.ENEMY;
        }
    }
}
