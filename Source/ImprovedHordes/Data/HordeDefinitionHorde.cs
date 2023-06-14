using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Data.XML;
using ImprovedHordes.Implementations.Logging;

namespace ImprovedHordes.Data
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
            if(ImprovedHordesLoggerFactory.TryGetInstance(out var loggerFactory))
                return new HordeDefinitionEntityGenerator(loggerFactory, playerGroup, this.definition);

            return null;
        }

        public abstract HordeType GetHordeType();
    }
}
