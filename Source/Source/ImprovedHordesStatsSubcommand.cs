﻿using ImprovedHordes.Source;
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
                int requestsCount = core.GetMainThreadRequestProcessor().GetRequestCount();
                int totalCount = 0;

                message = "WorldHordeClusterTracker: ";
                foreach (var clusterEntry in core.GetHordeManager().GetClusterTracker().GetClusters())
                {
                    message += $"{clusterEntry.Key.Name} - ({clusterEntry.Value.Count}) ";
                    totalCount += clusterEntry.Value.Count;
                }

                message += $"\nTotal Count {totalCount}";

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