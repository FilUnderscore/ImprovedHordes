namespace ImprovedHordes.Horde
{
    abstract class HordeGenerator
    {
        protected string type;
        public HordeGenerator(string type)
        {
            this.type = type;
        }

        public abstract Horde GenerateHorde(PlayerHordeGroup group);
    }
}
