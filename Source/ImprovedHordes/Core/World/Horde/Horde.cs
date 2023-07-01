using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.World.Horde.Characteristics;

namespace ImprovedHordes.Core.World.Horde
{
    public interface IHorde
    {
        HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup, IRandom random);
        HordeCharacteristics CreateCharacteristics();

        bool CanMergeWith(IHorde other);
        HordeType GetHordeType();
    }
}