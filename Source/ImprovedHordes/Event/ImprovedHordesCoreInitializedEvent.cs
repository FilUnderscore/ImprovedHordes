using ImprovedHordes.Core;
using System;

namespace ImprovedHordes.Event
{
    public sealed class ImprovedHordesCoreInitializedEvent : EventArgs
    {
        private readonly ImprovedHordesCore core;

        public ImprovedHordesCoreInitializedEvent(ImprovedHordesCore core)
        {
            this.core = core;
        }

        public ImprovedHordesCore GetCore()
        {
            return this.core;
        }
    }
}
