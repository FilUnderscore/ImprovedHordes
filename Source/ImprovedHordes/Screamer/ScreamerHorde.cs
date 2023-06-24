using ImprovedHordes.Core.World.Horde;
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
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(0.8f, 1.6f), new SensitivityHordeCharacteristic(4.0f));
        }

        public override HordeType GetHordeType()
        {
            return HordeType.ENEMY;
        }

        public override bool CanMergeWith(IHorde other)
        {
            if (other.GetType() == typeof(ScreamerHorde)) // Prevent screamers from merging with screamers, otherwise you get too many screamers.
                return false;

            return false;
        }
    }
}