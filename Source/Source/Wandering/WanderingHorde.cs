using ImprovedHordes.Source.Core.Horde;
using System;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WanderingHorde : IHorde
    {
        private static readonly HordeEntityGenerator GENERATOR = new WanderingHordeGenerator();

        public HordeEntityGenerator GetEntityGenerator()
        {
            return GENERATOR;
        }

        public float GetSensitivity()
        {
            return 1.0f;
        }

        public float GetWalkSpeed()
        {
            return 1.6f;
        }
    }
}
