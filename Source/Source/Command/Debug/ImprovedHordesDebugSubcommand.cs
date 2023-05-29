#if DEBUG
using ImprovedHordes.Command;
using ImprovedHordes.Source.Command.Debug;

namespace ImprovedHordes.Source
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