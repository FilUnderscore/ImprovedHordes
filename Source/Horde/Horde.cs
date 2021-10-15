namespace ImprovedHordes.Horde
{
    public class Horde
    {
        public readonly HordeGroup group;
        public readonly int count;
        public readonly bool feral;

        public readonly int[] entities;

        public Horde(HordeGroup group, int count, bool feral, int[] entities)
        {
            this.group = group;
            this.count = count;
            this.feral = feral;
            this.entities = entities;
        }

        public Horde(Horde horde) : this(horde.group, horde.count, horde.feral, horde.entities) { }
    }
}
