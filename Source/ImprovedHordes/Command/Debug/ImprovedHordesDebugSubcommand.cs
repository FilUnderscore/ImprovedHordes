#if DEBUG
using ImprovedHordes.Core.Command;

namespace ImprovedHordes.Command.Debug
{
    internal class ImprovedHordesDebugSubcommand : SubcommandBase
    {
        public ImprovedHordesDebugSubcommand() : base("debug")
        {
            RegisterSubcommand(new ImprovedHordesDebugServerSubcommand());
        }

        public override string GetDescription()
        {
            return "Debug related commands.";
        }
    }
}
#endif