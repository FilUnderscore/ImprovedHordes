using ImprovedHordes.Source.Horde.AI;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Horde
{
    public interface IHorde
    {
        HordeEntityGenerator GetEntityGenerator();

        float GetSensitivity();
        float GetWalkSpeed();
    }
}