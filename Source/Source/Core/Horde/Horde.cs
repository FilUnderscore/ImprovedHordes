using ImprovedHordes.Source.Core.Horde.World;

namespace ImprovedHordes.Source.Core.Horde
{
    public interface IHorde
    {
        HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup);

        float GetSensitivity();
        float GetWalkSpeed();
    }
}