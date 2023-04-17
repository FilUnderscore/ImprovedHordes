namespace ImprovedHordes.Source.Core.Horde
{
    public interface IHorde
    {
        HordeEntityGenerator GetEntityGenerator();

        float GetSensitivity();
        float GetWalkSpeed();
    }
}