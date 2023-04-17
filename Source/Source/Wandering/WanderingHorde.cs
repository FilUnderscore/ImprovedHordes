using ImprovedHordes.Source.Core.Horde;
using ImprovedHordes.Source.Core.Horde.World;

namespace ImprovedHordes.Source.Wandering
{
    public sealed class WanderingHorde : IHorde
    {
        public HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup)
        {
            return new HordeDefinitionEntityGenerator(playerGroup, "wandering");
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
