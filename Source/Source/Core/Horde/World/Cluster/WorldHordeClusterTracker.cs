using HarmonyLib;
using ImprovedHordes.Source.Core.Horde.World.Event;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde.AI.Commands;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class WorldHordeClusterTracker
    {
        private const int MERGE_DISTANCE = 100;

        private struct PlayerSnapshot
        {
            public Vector3 location;
            public int gamestage;

            public PlayerSnapshot(Vector3 location, int gamestage)
            {
                this.location = location;
                this.gamestage = gamestage;
            }
        }

        private Task<int> UpdateTask;

        private ConcurrentQueue<HordeCluster> toAdd = new ConcurrentQueue<HordeCluster>();
        private ConcurrentQueue<HordeCluster> toRemove = new ConcurrentQueue<HordeCluster>();

        private int clusterCount = 0;
        private List<HordeCluster> clusters = new List<HordeCluster>();

        private List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();
        private List<WorldEventReportEvent> eventsToReport = new List<WorldEventReportEvent>();

        public WorldHordeClusterTracker(WorldEventReporter reporter)
        {
            reporter.OnWorldEventReport += Reporter_OnWorldEventReport;
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
                    //Log.Out("Added cluster");
                }

                // Remove dead/merged clusters.
                while(toRemove.TryDequeue(out HordeCluster cluster))
                {
                    clusters.Remove(cluster);
                    Log.Out("Removed cluster");
                }

                // Update cluster count.
                this.clusterCount = this.clusters.Count;

                int eventsProcessed = UpdateTask.Result;

                if(eventsProcessed > 0)
                    this.eventsToReport.RemoveRange(0, eventsProcessed);
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

        public int GetClusterCount()
        {
            return this.clusterCount;
        }

        private async Task<int> UpdateAsync(List<PlayerSnapshot> players, List<WorldEventReportEvent> eventReports, float dt)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(this.clusters, cluster =>
                {
                    IEnumerable<PlayerSnapshot> nearby = players.Where(player =>
                    {
                        return Vector3.Distance(player.location, cluster.GetLocation()) <= 90;
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
                                bool nearby = Vector3.Distance(cluster.GetLocation(), otherCluster.GetLocation()) <= MERGE_DISTANCE;

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