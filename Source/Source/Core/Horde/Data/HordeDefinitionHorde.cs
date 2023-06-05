using ImprovedHordes.Source.Core.Horde.Data.XML;
using ImprovedHordes.Source.Core.Horde.World;
using ImprovedHordes.Source.Core.Horde.World.Cluster;

namespace ImprovedHordes.Source.Core.Horde.Data
{
    public abstract class HordeDefinitionHorde : IHorde
    {
        private readonly HordeDefinition definition;

        public HordeDefinitionHorde(string type)
        {
            this.definition = HordesFromXml.GetHordeDefinition(type);
        }

        public bool CanMergeWith(IHorde other)
        {
            if (other.GetType().Equals(this.GetType())) // Same horde types at runtime should always be able to merge together.
                return true;

            if (!(other is HordeDefinitionHorde otherHordeDefinitionHorde))
                return other.CanMergeWith(this); // Watch out for Stack Overflow, other horde type must implement without calling the inverse.

            return this.definition.CanMergeWith(otherHordeDefinitionHorde.definition);
        }

        public abstract HordeCharacteristics CreateCharacteristics();

        public HordeEntityGenerator CreateEntityGenerator(PlayerHordeGroup playerGroup)
        {
            return new HordeDefinitionEntityGenerator(playerGroup, this.definition);
        }
    }
}
