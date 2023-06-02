using ImprovedHordes.Source.Core.Horde;
using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Horde.World;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Horde.World.Cluster.Characteristics;

namespace ImprovedHordes.Source.Scout
{
    public sealed class ScoutHorde : IHorde
    {
        public HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.6f), new SensitivityHordeCharacteristic(10.0f));
        }

        public HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup)
        {
            return new HordeDefinitionEntityGenerator(playerGroup, "screamer");
        }
    }
}