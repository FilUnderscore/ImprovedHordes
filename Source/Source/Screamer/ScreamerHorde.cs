using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Horde.Data;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Cluster.Characteristics;

namespace ImprovedHordes.Source.Scout
{
    public sealed class ScreamerHorde : HordeDefinitionHorde
    {
        public ScreamerHorde() : base("screamer")
        {
        }

        public override HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.6f, 3.2f), new SensitivityHordeCharacteristic(4.0f));
        }
    }
}