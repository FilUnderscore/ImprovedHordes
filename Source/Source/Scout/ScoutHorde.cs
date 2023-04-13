using ImprovedHordes.Source.Core.Horde;

namespace ImprovedHordes.Source.Scout
{
    public sealed class ScoutHorde : IHorde
    {
        private static readonly HordeEntityGenerator GENERATOR = new ScoutHordeGenerator();

        public HordeEntityGenerator GetEntityGenerator()
        {
            return GENERATOR;
        }

        public float GetSensitivity()
        {
            return 1.0f;
        }
    }
}