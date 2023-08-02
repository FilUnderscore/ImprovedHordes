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
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(0.16f, 0.32f), new SensitivityHordeCharacteristic(1.0f));
        }

        public override HordeType GetHordeType()
        {
            return HordeType.ENEMY;
        }
    }
}
