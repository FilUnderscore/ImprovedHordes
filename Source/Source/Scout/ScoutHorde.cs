using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Horde.Data;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Cluster.Characteristics;

namespace ImprovedHordes.Source.Scout
{
    public sealed class ScoutHorde : HordeDefinitionHorde
    {
        public ScoutHorde() : base("screamer")
        {
        }

        public override HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.6f), new SensitivityHordeCharacteristic(10.0f));
        }
    }
}