using ImprovedHordes.Source;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Scout;
using System.Collections.Generic;

namespace ImprovedHordes.Command
{
    internal class ImprovedHordesTestSubcommand : ExecutableSubcommandBase
    {
        public ImprovedHordesTestSubcommand() : base("test")
        {
        }

        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            if (ImprovedHordesCore.TryGetInstance(out ImprovedHordesCore core))
            {
                core.GetHordeManager().GetSpawner().Spawn<ScoutHorde, PlayerHordeSpawn>(new PlayerHordeSpawn(GameManager.Instance.World.GetPrimaryPlayer(), 50));
                message = "Spawned test";
            }
            else
            {
                message = "Null instance";
            }

            return false;
        }

        public override (string name, bool optional)[] GetArgs()
        {
            return null;
        }

        public override string GetDescription()
        {
            return "Spawns a test horde.";
        }
    }
}