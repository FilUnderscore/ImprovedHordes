using ImprovedHordes.Core.Command;
using ImprovedHordes.Core.Threading;
using System;
using System.Collections.Generic;
using static ImprovedHordes.Core.World.Horde.WorldHordeTracker;

namespace ImprovedHordes.Command
{
    internal class ImprovedHordesStatsSubcommand : ExecutableSubcommandBase
    {
        private ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> clusters;

        public ImprovedHordesStatsSubcommand() : base("stats")
        {
        }

        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            if (ImprovedHordesMod.TryGetInstance(out ImprovedHordesMod mod))
            {
                int requestsCount = mod.GetCore().GetMainThreadRequestProcessor().GetRequestCount();
                int totalCount = 0;
                float totalDensity = 0.0f;

                message = "World Horde Clusters: ";

                if (this.clusters == null)
                    this.clusters = mod.GetCore().GetWorldHordeTracker().GetClustersSubscription().Subscribe();

                if (this.clusters.TryGet(out var clusters))
                {
                    foreach (var clusterEntry in clusters)
                    {
                        float totalClusterTypeDensity = 0.0f;

                        message += $"\n    {clusterEntry.Key.Name}: {clusterEntry.Value.Count}";
                        totalCount += clusterEntry.Value.Count;

                        foreach(var cluster in clusterEntry.Value)
                        {
                            totalClusterTypeDensity += cluster.density;
                        }

                        message += $" (Total Density: {totalClusterTypeDensity})";
                        totalDensity += totalClusterTypeDensity;
                    }
                }

                message += $"\nTotal Count: {totalCount} (Total Density: {totalDensity})";

                message += $"\nMainThreadRequestProcessor - Main thread requests being processed: {requestsCount}";
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