using ImprovedHordes.Core.Command;
using ImprovedHordes.Implementations.Logging;
using System.Collections.Generic;

namespace ImprovedHordes.Command
{
    internal sealed class ImprovedHordesVerboseSubcommand : ExecutableSubcommandBase
    {
        public ImprovedHordesVerboseSubcommand() : base("verbose") { }
        
        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            ImprovedHordesLogger.VERBOSE = !ImprovedHordesLogger.VERBOSE;

            if(ImprovedHordesLogger.VERBOSE) 
            {
                message = "Enabled verbose logging.";
            }
            else
            {
                message = "Disabled verbose logging.";
            }

            return false;
        }

        public override (string name, bool optional)[] GetArgs()
        {
            return null;
        }

        public override string GetDescription()
        {
            return "Enables/Disables verbose logging.";
        }
    }
}
