using ImprovedHordes.Core.Command;
using System.Collections.Generic;

namespace ImprovedHordes.Command
{
    internal sealed class ImprovedHordesFlushSubcommand : ExecutableSubcommandBase
    {
        public ImprovedHordesFlushSubcommand() : base("flush")
        {
        }

        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            if(ImprovedHordesMod.TryGetInstance(out var mod))
            {
                mod.GetCore().Flush();
                message = "Successfully flushed world.";
            }
            else
            {
                message = "Failed to flush world.";
            }

            return false;
        }

        public override (string name, bool optional)[] GetArgs()
        {
            return null;
        }

        public override string GetDescription()
        {
            return "Kill all existing hordes and re-populate the world.";
        }
    }
}
