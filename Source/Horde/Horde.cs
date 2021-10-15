﻿namespace ImprovedHordes.Horde
{
    public class Horde
    {
        public HordeGroup group;
        public int count;
        public bool feral;

        public int[] entities;

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