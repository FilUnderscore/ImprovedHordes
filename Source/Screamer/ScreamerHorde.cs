using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Data;

namespace ImprovedHordes.Screamer
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