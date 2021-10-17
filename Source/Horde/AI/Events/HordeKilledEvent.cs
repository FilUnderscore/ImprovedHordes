using System;

namespace ImprovedHordes.Horde.AI.Events
{
    public class HordeKilledEvent : EventArgs
    {
        public readonly Horde horde;

        public HordeKilledEvent(Horde horde)
        {
            this.horde = horde;
        }
    }
}
