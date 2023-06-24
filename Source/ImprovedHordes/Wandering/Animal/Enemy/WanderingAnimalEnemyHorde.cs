using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Data;

namespace ImprovedHordes.Wandering.Animal.Enemy
{
    public sealed class WanderingAnimalEnemyHorde : HordeDefinitionHorde
    {
        public WanderingAnimalEnemyHorde() : base("wandering_animal_enemy") {}

        public override HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.6f, 1.0f));
        }

        public override HordeType GetHordeType()
        {
            return HordeType.ANIMAL;
        }
    }
}
