using ImprovedHordes.Source.Core.Horde;
using ImprovedHordes.Source.Core.Horde.World;

namespace ImprovedHordes.Source.Scout
{
    public sealed class ScoutHorde : IHorde
    {
        public HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup)
        {
            return new HordeDefinitionEntityGenerator(playerGroup, "screamer");
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