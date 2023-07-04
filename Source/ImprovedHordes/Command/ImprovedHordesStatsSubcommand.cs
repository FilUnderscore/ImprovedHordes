using ImprovedHordes.Core.Command;
using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.POI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Command
{
    internal class ImprovedHordesStatsSubcommand : ExecutableSubcommandBase
    {
        private ThreadSubscriber<List<PlayerHordeGroup>> playerGroups;
        private ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> clusters;

        public ImprovedHordesStatsSubcommand() : base("stats")
        {
        }

        [Flags]
        private enum StatFlag
        {
            Cluster = 1,
            Request = 2,
            Zone = 4,
            Player = 8,
            All = ~0
        }

        private static bool IsStatFlagSet(StatFlag flag, StatFlag target)
        {
            return (flag & target) == target;
        }

        public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
        {
            if (ImprovedHordesMod.TryGetInstance(out ImprovedHordesMod mod))
            {
                StatFlag flags = StatFlag.All;

                if (args.Count == 1)
                {
                    string flag = args[0];

                    if(!Enum.TryParse(flag, true, out flags))
                    {
                        message = $"Invalid flag '{flag}'. Valid flags: ";

                        var values = Enum.GetValues(typeof(StatFlag));
                        int count = values.Length;
                        
                        for(int i = 0; i < count; i++)
                        {
                            message += Enum.GetName(typeof(StatFlag), values.GetValue(i)) + (i < count - 1 ? ", " : "");
                        }

                        return false;
                    }
                }

                if(IsStatFlagSet(flags, StatFlag.Cluster))
                {
                    GetClusterStats(mod, ref message);
                }

                if(IsStatFlagSet(flags, StatFlag.Request))
                {
                    GetRequestStats(mod, ref message);
                }

                if(IsStatFlagSet(flags, StatFlag.Zone))
                {
                    GetZoneStats(mod, ref message);
                }

                if(IsStatFlagSet(flags, StatFlag.Player))
                {
                    GetPlayerStats(mod, ref message);
                }
            }
            else
            {
                message = "Null instance";
            }

            return false;
        }

        private void GetClusterStats(ImprovedHordesMod mod, ref string message)
        {
            message = "\nWorld Horde Clusters Being Tracked:";

            if (this.clusters == null)
                this.clusters = mod.GetCore().GetWorldHordeTracker().GetClustersSubscription().Subscribe();

            if (this.clusters.TryGet(out var clusters))
            {
                int totalCount = 0;
                float totalDensity = 0.0f;

                foreach (var clusterEntry in clusters)
                {
#if !DEBUG
                        if (clusterEntry.Value.Count == 0)
                            continue;
#endif

                    float totalClusterTypeDensity = 0.0f;

                    message += $"\n    {clusterEntry.Key.Name}: {clusterEntry.Value.Count}";
                    totalCount += clusterEntry.Value.Count;

                    foreach (var cluster in clusterEntry.Value)
                    {
                        totalClusterTypeDensity += cluster.density;
                    }

                    message += $" (Total Density: {totalClusterTypeDensity})";
                    totalDensity += totalClusterTypeDensity;
                }

                message += $"\nTotal Count: {totalCount} (Total Density: {totalDensity})";
            }
            else
            {
                message += "\n    Failed to retrieve latest cluster information.";
            }
        }

        private void GetRequestStats(ImprovedHordesMod mod, ref string message)
        {
            message += "\nMain Thread Requests Being Processed:";

            Dictionary<Type, int> requestCounts = mod.GetCore().GetMainThreadRequestProcessor().GetRequestCounts();
            int totalCount = 0;

            foreach(var requestEntry in requestCounts) 
            {
                string requestTypeName = requestEntry.Key.Name;
                int count = requestEntry.Value;

                message += $"\n    {requestTypeName}: {count}";

                totalCount += count;
            }

            message += $"\nTotal Count: {totalCount}";
        }

        private void GetZoneStats(ImprovedHordesMod mod, ref string message)
        {
            message += "\nCurrent POI Zone Info:";

            if (GameManager.Instance.World.GetPrimaryPlayer() != null)
            {
                const float MIN_ZONE_EDGE_DISTANCE = 20.0f;

                Vector3 playerPos = GameManager.Instance.World.GetPrimaryPlayer().position;
                ICollection<WorldPOIScanner.POIZone> nearbyZones = mod.GetPOIScanner().GetZones().Where(z => Vector3.Distance(playerPos, z.GetBounds().ClosestPoint(playerPos)) <= MIN_ZONE_EDGE_DISTANCE).ToList();

                if (!nearbyZones.Any())
                {
                    message += "\n    Not currently near a POI zone.";
                    return;
                }

                WorldPOIScanner.POIZone zone = nearbyZones.First();
                Bounds zoneBounds = zone.GetBounds();

                message += $"\n    Size: {zoneBounds.size.magnitude / 2}";
                message += $"\n    Center: {zoneBounds.center}";
                message += $"\n    POI Count: {zone.GetCount()}";
                message += $"\n    Density: {zone.GetDensity()}";
            }
            else
            {
                message += "\n    Not currently near a POI zone.";
            }
        }

        private void GetPlayerStats(ImprovedHordesMod mod, ref string message)
        {
            message += "\nCurrent Player Horde Groups Being Tracked:";

            if (this.playerGroups == null)
                this.playerGroups = mod.GetCore().GetWorldHordeTracker().GetPlayerTracker().Subscribe();

            if(this.playerGroups.TryGet(out var playerGroups))
            {
                foreach(var playerGroup in playerGroups)
                {
                    message += $"\n";

                    int gamestage = playerGroup.GetGamestage();
                    string biome = playerGroup.GetBiome();
                    
                    List<PlayerSnapshot> players = playerGroup.GetPlayers();

                    for(int i = 0; i < players.Count; i++)
                    {
                        message += $"{players[i].player.EntityName} ({players[i].player.gameStage})" + (i < players.Count - 1 ? ", " : "");
                    }

                    message += $"\n    Gamestage: {gamestage}";
                    message += $"\n    Biome: {biome}";
                }
            }
            else
            {
                message += "\n    Failed to retrieve latest player group information.";
            }
        }

        public override (string name, bool optional)[] GetArgs()
        {
            return new (string name, bool optional)[] { ("type", true) };
        }

        public override string GetDescription()
        {
            return "Stats regarding Improved Hordes systems.";
        }
    }
}