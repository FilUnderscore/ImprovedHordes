using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Data;

namespace ImprovedHordes.Wandering.Animal
{
    public sealed class WanderingAnimalHorde : HordeDefinitionHorde
    {
        public WanderingAnimalHorde() : base("wandering_animal") {}

        public override HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(0.8f, 0.0f));
        }

        public override HordeType GetHordeType()
        {
            return HordeType.ANIMAL;
        }
    }
}
