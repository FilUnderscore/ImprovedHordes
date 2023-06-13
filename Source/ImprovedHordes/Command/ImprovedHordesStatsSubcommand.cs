using ImprovedHordes.Core.Command;
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
            if (ImprovedHordesMod.TryGetInstance(out ImprovedHordesMod mod))
            {
                int requestsCount = mod.GetCore().GetMainThreadRequestProcessor().GetRequestCount();
                int totalCount = 0;

                message = "WorldHordeTracker Clusters: ";
                foreach (var clusterEntry in mod.GetCore().GetWorldHordeTracker().GetClusters())
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
            return new (string name, bool optional)[] { ("horde type", true) };
        }

        public override string GetDescription()
        {
            return "Stats regarding Improved Hordes core systems.";
        }
    }
}