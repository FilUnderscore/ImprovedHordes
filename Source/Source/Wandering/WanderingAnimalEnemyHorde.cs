using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Horde.Data;
using ImprovedHordes.Source.Core.Horde.World.Cluster;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WanderingAnimalEnemyHorde : HordeDefinitionHorde
    {
        public WanderingAnimalEnemyHorde() : base("wandering_animal_enemy") {}

        public override HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.6f, 2.0f));
        }
    }
}
