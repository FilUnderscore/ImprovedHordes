using HarmonyLib;
using ImprovedHordes.Source.Core.Horde.World.Event;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde.AI.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class WorldHordeClusterTracker
    {
        private const int MERGE_DISTANCE_LOADED = 10;
        private const int MERGE_DISTANCE_UNLOADED = 100;

        private readonly int VIEW_DISTANCE = 90;

        private readonly struct PlayerSnapshot
        {
            public readonly Vector3 location;
            public readonly int gamestage;

            public PlayerSnapshot(Vector3 location, int gamestage)
            {
                this.location = location;
                this.gamestage = gamestage;
            }
        }

        public readonly struct ClusterSnapshot
        {
            public readonly IHorde horde;
            public readonly Vector3 location;
            public readonly float density;

            public ClusterSnapshot(IHorde horde, Vector3 location, float density)
            {
                this.horde = horde;
                this.location = location;
                this.density = density;
            }
        }

        private Task<int> UpdateTask;

        // Shared
        private readonly ConcurrentQueue<HordeCluster> toAdd = new ConcurrentQueue<HordeCluster>();
        private readonly ConcurrentQueue<HordeCluster> toRemove = new ConcurrentQueue<HordeCluster>();

        // Personal (main-thread), updated after task is completed.
        private readonly List<HordeCluster> clusters = new List<HordeCluster>();

        private readonly List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();
        private readonly List<WorldEventReportEvent> eventsToReport = new List<WorldEventReportEvent>();

        private readonly Dictionary<Type, List<ClusterSnapshot>> clusterSnapshots = new Dictionary<Type, List<ClusterSnapshot>>();

        public WorldHordeClusterTracker(WorldEventReporter reporter)
        {
            reporter.OnWorldEventReport += Reporter_OnWorldEventReport;

            this.RegisterHordes();
        }

        private void RegisterHordes()
        {
            var type = typeof(IHorde);
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => type.IsAssignableFrom(p) && !p.IsInterface);

            foreach(var hordeType in types)
            {
                clusterSnapshots.Add(hordeType, new List<ClusterSnapshot>());
            }
        }

        private void Reporter_OnWorldEventReport(object sender, WorldEventReportEvent e)
        {
            Log.Out($"Pos {e.GetLocation()} Interest {e.GetInterest()} Dist {e.GetDistance()}");
            this.eventsToReport.Add(e);
        }

        public void Update(float dt)
        {
            if(UpdateTask != null && UpdateTask.IsCompleted)
            {
                // Clear old snapshots after task is complete.
                snapshots.Clear();

                // Add clusters.
                while(toAdd.TryDequeue(out HordeCluster cluster))
                {
                    clusters.Add(cluster);
                }

                // Remove dead/merged clusters.
                while(toRemove.TryDequeue(out HordeCluster cluster))
                {
                    clusters.Remove(cluster);
                    Log.Out("Removed cluster");
                }

                int eventsProcessed = UpdateTask.Result;

                if(eventsProcessed > 0)
                    this.eventsToReport.RemoveRange(0, eventsProcessed);

                // Update cluster snapshots and remove outdated ones.
                
                foreach(var key in clusterSnapshots.Keys)
                {
                    clusterSnapshots[key].Clear();
                }

                foreach(var cluster in this.clusters)
                {
                    clusterSnapshots[cluster.GetHorde().GetType()].Add(new ClusterSnapshot(cluster.GetHorde(), cluster.GetLocation(), cluster.GetDensity()));
                }
            }

            if(UpdateTask == null || UpdateTask.IsCompleted)
            {
                foreach(var player in GameManager.Instance.World.Players.list)
                {
                    snapshots.Add(new PlayerSnapshot(player.position, player.gameStage));
                }

                this.UpdateTask = Task.Run(async () =>
                {
                    return await UpdateAsync(snapshots, eventsToReport.ToList(), dt);
                });
            }
        }

        public List<ClusterSnapshot> GetClustersOf<Horde>() where Horde: IHorde
        {
            return this.clusterSnapshots[typeof(Horde)];
        }

        public Dictionary<Type, List<ClusterSnapshot>> GetClusters()
        {
            return this.clusterSnapshots;
        }

        private async Task<int> UpdateAsync(List<PlayerSnapshot> players, List<WorldEventReportEvent> eventReports, float dt)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(this.clusters, cluster =>
                {
                    IEnumerable<PlayerSnapshot> nearby = players.Where(player =>
                    {
                        float distance = cluster.IsSpawned() ? VIEW_DISTANCE + 20 : VIEW_DISTANCE;
                        return Vector3.Distance(player.location, cluster.GetLocation()) <= distance;
                    });

                    if((nearby.Any() && !cluster.IsSpawned()) || !cluster.IsDensityMatchedWithEntityCount())
                    {
                        PlayerHordeGroup group = new PlayerHordeGroup();
                        nearby.Do(player => group.AddPlayer(player.gamestage));

                        cluster.Spawn(group);
                    }
                    else if(!nearby.Any() && cluster.IsSpawned())
                    {
                        cluster.Despawn();
                    }

                    if(cluster.IsSpawned())
                    {
                        cluster.UpdatePosition();
                    }

                    if(cluster.IsDead())
                    {
                        toRemove.Enqueue(cluster);
                    }
                    else
                    {
                        // Tick AI.
                        IEnumerable<WorldEventReportEvent> nearbyReports = eventReports.Where(report =>
                        {
                            return Vector3.Distance(report.GetLocation(), cluster.GetLocation()) <= report.GetDistance() * cluster.GetHorde().GetSensitivity();
                        });

                        if(nearbyReports.Any())
                        {
                            // Interrupt AI to split off/target reported event.
                            WorldEventReportEvent nearbyEvent = nearbyReports.OrderBy(report => report.GetDistance()).First();
                            cluster.Queue(true, new GoToTargetAICommand(nearbyEvent.GetLocation()));
                        }

                        cluster.Update(dt);
                    }
                });

                // Merge nearby clusters.
                for(int index = 0; index < clusters.Count; index++)
                {
                    HordeCluster cluster = clusters[index];

                    if (!cluster.IsDead())
                    {
                        for(int j = index + 1; j < clusters.Count; j++)
                        {
                            HordeCluster otherCluster = clusters[j];

                            if (!otherCluster.IsDead())
                            {
                                int mergeDistance = cluster.IsSpawned() ? MERGE_DISTANCE_LOADED : MERGE_DISTANCE_UNLOADED;
                                bool nearby = Vector3.Distance(cluster.GetLocation(), otherCluster.GetLocation()) <= mergeDistance;

                                if (nearby && cluster.Merge(otherCluster))
                                {
                                    toRemove.Enqueue(otherCluster);
                                }
                            }
                        }
                    }
                }
            });

            return eventReports.Count;
        }

        public void Add(HordeCluster cluster)
        {
            toAdd.Enqueue(cluster);
        }

        private class LogReportRequest : IMainThreadRequest
        {
            private readonly string str;

            public LogReportRequest(string str)
            {
                this.str = str;
            }

            public bool IsDone()
            {
                return true;
            }

            public void TickExecute()
            {
                Log.Out(this.str);
            }
        }
    }
}