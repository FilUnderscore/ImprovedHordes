using ImprovedHordes.Command.Debug;
using ImprovedHordes.Core.Command;

namespace ImprovedHordes.Command
{
    class ImprovedHordesCommand : ConsoleCommandBase
    {
        public ImprovedHordesCommand() : base("improvedhordes", "[Improved Hordes]", "ih")
        {
            RegisterSubcommand(new ImprovedHordesTestSubcommand());
            RegisterSubcommand(new ImprovedHordesStatsSubcommand());

#if DEBUG
            // Register debug related subcommands.
            RegisterSubcommand(new ImprovedHordesDebugSubcommand());
#endif
        }

        public override string GetDescription()
        {
            return "Execute a function from the Improved Hordes Mod. `help improvedhordes` for more information.";
        }
    }
}
