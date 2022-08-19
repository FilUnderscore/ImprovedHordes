using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.Command
{
    internal class ImprovedHordesListSubcommand : ExecutableSubcommandBase
    {
        public ImprovedHordesListSubcommand() : base("list")
        {
        }

        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            var allHordes = ImprovedHordesManager.Instance.HordeManager.GetAllHordes();

            StringBuilder builder = new StringBuilder();

            if (allHordes.Count > 0)
            {
                foreach (var entry in allHordes)
                {
                    var playerGroup = entry.Key;
                    var hordes = entry.Value;

                    builder.AppendLine(playerGroup.ToString());

                    foreach (var horde in hordes)
                    {
                        builder.AppendLine(" - " + horde.ToString());
                    }
                }
            }
            else
            {
                builder.AppendLine("No hordes are currently occurring.");
            }

            message = builder.ToString();
            return false;
        }

        public override (string name, bool optional)[] GetArgs()
        {
            return null;
        }

        public override string GetDescription()
        {
            return "Shows all player groups and their associated hordes.";
        }
    }
}