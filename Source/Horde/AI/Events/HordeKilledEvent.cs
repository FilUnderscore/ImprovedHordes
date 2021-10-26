using System;

namespace ImprovedHordes.Horde.AI.Events
{
    public class HordeKilledEvent : EventArgs
    {
        public readonly HordeAIHorde horde;

        public HordeKilledEvent(HordeAIHorde horde)
        {
            this.horde = horde;
        }
    }
}
