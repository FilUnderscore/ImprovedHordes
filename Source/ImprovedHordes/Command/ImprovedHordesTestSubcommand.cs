using ImprovedHordes.Core.Command;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.Screamer;
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
            if (ImprovedHordesMod.TryGetInstance(out ImprovedHordesMod mod))
            {
                EntityPlayerLocal player = GameManager.Instance.World.GetPrimaryPlayer();

                mod.GetCore().GetWorldHordeSpawner().Spawn<ScreamerHorde, PlayerHordeSpawn>(new PlayerHordeSpawn(player, 100), new HordeSpawnData(40), null, null);
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