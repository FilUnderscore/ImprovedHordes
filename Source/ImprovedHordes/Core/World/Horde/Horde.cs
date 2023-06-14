using ImprovedHordes.Core.World.Horde.Characteristics;

namespace ImprovedHordes.Core.World.Horde
{
    public interface IHorde
    {
        HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup);
        HordeCharacteristics CreateCharacteristics();

        bool CanMergeWith(IHorde other);
        HordeType GetHordeType();
    }
}