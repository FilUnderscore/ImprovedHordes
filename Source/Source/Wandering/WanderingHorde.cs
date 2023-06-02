using ImprovedHordes.Source.Core.Horde;
using ImprovedHordes.Source.Core.Horde.Characteristics;
using ImprovedHordes.Source.Core.Horde.World;
using ImprovedHordes.Source.Core.Horde.World.Cluster.Characteristics;
using ImprovedHordes.Source.Core.Horde.World.Cluster;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WanderingHorde : IHorde
    {
        public HordeCharacteristics CreateCharacteristics()
        {
            return new HordeCharacteristics(new WalkSpeedHordeCharacteristic(1.6f), new SensitivityHordeCharacteristic(1.0f));
        }

        public HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup)
        {
            return new HordeDefinitionEntityGenerator(playerGroup, "wandering");
        }
    }
}
