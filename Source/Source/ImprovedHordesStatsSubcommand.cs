using ImprovedHordes.Source;
using System.Collections.Generic;

namespace ImprovedHordes.Command
{
    internal class ImprovedHordesStatsSubcommand : ExecutableSubcommandBase
    {
        public ImprovedHordesStatsSubcommand() : base("stats")
        {
        }

        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            if (ImprovedHordesCore.TryGetInstance(out ImprovedHordesCore core))
            {
                int clusterCount = core.GetHordeManager().GetClusterTracker().GetClusterCount();
                int requestsCount = core.GetMainThreadRequestProcessor().GetRequestCount();

                message = $"WorldHordeClusterTracker: Total clusters ({clusterCount})";
                message += $"\nMainThreadRequestProcessor: Main thread requests being processed {requestsCount}";
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
            return "Stats regarding Improved Hordes core systems.";
        }
    }
}