using ImprovedHordes.Command;

namespace ImprovedHordes
{
    class ImprovedHordesCommand : ConsoleCommandBase
    {
        public ImprovedHordesCommand() : base("improvedhordes", "[Improved Hordes]", "ih")
        {
            RegisterSubcommand(new ImprovedHordesTestSubcommand());
        }

        public override string GetDescription()
        {
            return "Execute a function from the Improved Hordes Mod. `help improvedhordes` for more information.";
        }
    }
}
