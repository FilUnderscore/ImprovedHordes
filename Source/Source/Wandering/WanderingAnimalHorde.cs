using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Horde.Data;
using ImprovedHordes.Source.Core.Horde.World.Cluster;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WanderingAnimalHorde : HordeDefinitionHorde
    {
        public WanderingAnimalHorde() : base("wandering_animal") {}

        public override HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.6f, 0.0f));
        }
    }
}
