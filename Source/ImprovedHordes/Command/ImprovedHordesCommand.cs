#if DEBUG
using ImprovedHordes.Command.Debug;
#endif
using ImprovedHordes.Core.Command;

namespace ImprovedHordes.Command
{
    class ImprovedHordesCommand : ConsoleCommandBase
    {
        public ImprovedHordesCommand() : base("improvedhordes", "[Improved Hordes]", "ih")
        {
            RegisterSubcommand(new ImprovedHordesFlushSubcommand());
            RegisterSubcommand(new ImprovedHordesStatsSubcommand());
            RegisterSubcommand(new ImprovedHordesVerboseSubcommand());

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
