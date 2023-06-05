using ImprovedHordes.Source.Core.Horde;
using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Horde.World;
using ImprovedHordes.Source.Core.Horde.World.Cluster.Characteristics;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.Data;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WanderingEnemyHorde : HordeDefinitionHorde
    {
        public WanderingEnemyHorde() : base("wandering_enemy")
        {

        }

        public override HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.6f), new SensitivityHordeCharacteristic(1.0f));
        }
    }
}
