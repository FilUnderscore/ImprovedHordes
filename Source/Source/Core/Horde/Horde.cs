using ImprovedHordes.Source.Core.Horde.World;
using ImprovedHordes.Source.Core.Horde.World.Cluster;

namespace ImprovedHordes.Source.Core.Horde
{
    public interface IHorde
    {
        HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup);
        HordeCharacteristics CreateCharacteristics();

        bool CanMergeWith(IHorde other);
    }
}