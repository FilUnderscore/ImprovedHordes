using ImprovedHordes.Command;

namespace ImprovedHordes
{
    class ImprovedHordesCommand : ConsoleCommandBase
    {
        public ImprovedHordesCommand() : base("improvedhordes", "[Improved Hordes]", "ih")
        {
            RegisterSubcommand(new ImprovedHordesWanderingSubcommand());
            RegisterSubcommand(new ImprovedHordesListSubcommand());
            RegisterSubcommand(new ImprovedHordesStatsSubcommand());
        }

        public override string GetDescription()
        {
            return "Execute a function from the Improved Hordes Mod. `help improvedhordes` for more information.";
        }
    }
}
