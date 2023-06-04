using ImprovedHordes.Command;

namespace ImprovedHordes
{
    class ImprovedHordesCommand : ConsoleCommandBase
    {
        public ImprovedHordesCommand() : base("improvedhordes-legacy", "[Improved Hordes Legacy]", "ih-legacy")
        {
            RegisterSubcommand(new ImprovedHordesWanderingSubcommand());
            RegisterSubcommand(new ImprovedHordesListSubcommand());
            RegisterSubcommand(new ImprovedHordesStatsSubcommand());
            RegisterSubcommand(new ImprovedHordesSpawnSubcommand());
        }

        public override string GetDescription()
        {
            return "Execute a function from the Improved Hordes Legacy Mod. `help improvedhordes-legacy` for more information.";
        }
    }
}
