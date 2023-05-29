﻿using HarmonyLib;
using ImprovedHordes.Source.Core.Horde.World.Event;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde.AI.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class WorldHordeTracker : MainThreadSynchronizedTask<int>
    {
        private const int MERGE_DISTANCE_LOADED = 10;
        private const int MERGE_DISTANCE_UNLOADED = 100;

        private readonly int VIEW_DISTANCE = 90;

        public readonly struct PlayerSnapshot
        {
            public readonly Vector3 location;
            public readonly int gamestage;
            public readonly string biome;

            public PlayerSnapshot(Vector3 location, int gamestage, BiomeDefinition biome)
            {
                this.location = location;
                this.gamestage = gamestage;
                this.biome = biome != null ? biome.m_sBiomeName : null;
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

        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;
        
        // Shared
        private readonly ConcurrentQueue<WorldHorde> toAdd = new ConcurrentQueue<WorldHorde>();
        private readonly ConcurrentQueue<WorldHorde> toRemove = new ConcurrentQueue<WorldHorde>();

        private readonly ConcurrentQueue<HordeClusterSpawnRequest> clusterSpawnRequests = new ConcurrentQueue<HordeClusterSpawnRequest>();

        // Personal (main-thread), updated after task is completed.
        private readonly List<WorldHorde> hordes = new List<WorldHorde>();

        private readonly List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();
        private readonly List<WorldEventReportEvent> eventsToReport = new List<WorldEventReportEvent>();

        private readonly Dictionary<Type, List<ClusterSnapshot>> clusterSnapshots = new Dictionary<Type, List<ClusterSnapshot>>();

        public WorldHordeTracker(MainThreadRequestProcessor mainThreadRequestProcessor, WorldEventReporter reporter)
        {
            this.mainThreadRequestProcessor = mainThreadRequestProcessor;

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


        public override void BeforeTaskRestart()
        {
            foreach (var player in GameManager.Instance.World.Players.list)
            {
                snapshots.Add(new PlayerSnapshot(player.position, player.gameStage, player.biomeStandingOn));
            }
        }

        public override void OnTaskFinish(int returnValue)
        {
            // Clear old snapshots after task is complete.
            snapshots.Clear();

            // Add hordes.
            while (toAdd.TryDequeue(out WorldHorde cluster))
            {
                hordes.Add(cluster);
            }

            // Remove dead/merged hordes.
            while (toRemove.TryDequeue(out WorldHorde cluster))
            {
                hordes.Remove(cluster);
            }

            int eventsProcessed = returnValue;

            if (eventsProcessed > 0)
                this.eventsToReport.RemoveRange(0, eventsProcessed);

            // Update cluster snapshots and remove outdated ones.

            foreach (var key in clusterSnapshots.Keys)
            {
                clusterSnapshots[key].Clear();
            }

            foreach (var horde in this.hordes)
            {
                foreach (var cluster in horde.GetClusters())
                {
                    clusterSnapshots[cluster.GetHorde().GetType()].Add(new ClusterSnapshot(cluster.GetHorde(), horde.GetLocation(), cluster.GetDensity()));
                }
            }
        }

        public override async Task<int> UpdateAsync(float dt)
        {
            return await UpdateTrackerAsync(snapshots, eventsToReport.ToList(), dt);
        }

        public List<ClusterSnapshot> GetClustersOf<Horde>() where Horde: IHorde
        {
            return this.clusterSnapshots[typeof(Horde)];
        }

        public Dictionary<Type, List<ClusterSnapshot>> GetClusters()
        {
            return this.clusterSnapshots;
        }

        public List<PlayerSnapshot> GetPlayers()
        {
            return this.snapshots;
        }

        private async Task<int> UpdateTrackerAsync(List<PlayerSnapshot> players, List<WorldEventReportEvent> eventReports, float dt)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(this.hordes, horde =>
                {
                    IEnumerable<PlayerSnapshot> nearby = players.Where(player =>
                    {
                        float distance = horde.IsSpawned() ? VIEW_DISTANCE + 20 : VIEW_DISTANCE;
                        return Vector3.Distance(player.location, horde.GetLocation()) <= distance;
                    });

                    if(nearby.Any() && (!horde.IsSpawned() || horde.HasClusterSpawnsWaiting()))
                    {
                        PlayerHordeGroup group = new PlayerHordeGroup();
                        nearby.Do(player => group.AddPlayer(player.gamestage, player.biome));

                        foreach(var spawnRequest in horde.RequestSpawns(group))
                        {
                            this.clusterSpawnRequests.Enqueue(spawnRequest);
                        }
                    }
                    else if(!nearby.Any() && horde.IsSpawned())
                    {
                        horde.Despawn();
                    }

                    if(horde.IsSpawned())
                    {
                        horde.UpdatePosition();
                    }

                    if(horde.IsDead())
                    {
                        toRemove.Enqueue(horde);
                    }
                    else
                    {
                        // Tick AI.
                        IEnumerable<WorldEventReportEvent> nearbyReports = eventReports.Where(report =>
                        {
                            return Vector3.Distance(report.GetLocation(), horde.GetLocation()) <= report.GetDistance() * horde.GetSensitivity();
                        });

                        if(nearbyReports.Any())
                        {
                            // Interrupt AI to split off/target reported event.
                            WorldEventReportEvent nearbyEvent = nearbyReports.OrderBy(report => report.GetDistance()).First();
                            horde.Queue(true, new GoToTargetAICommand(nearbyEvent.GetLocation()));
                        }

                        horde.Update(dt);
                    }
                });

                // Merge nearby hordes.
                for(int index = 0; index < this.hordes.Count; index++)
                {
                    WorldHorde horde = this.hordes[index];

                    if (!horde.IsDead())
                    {
                        for(int j = index + 1; j < this.hordes.Count; j++)
                        {
                            WorldHorde otherHorde = this.hordes[j];

                            if (!otherHorde.IsDead())
                            {
                                int mergeDistance = horde.IsSpawned() ? MERGE_DISTANCE_LOADED : MERGE_DISTANCE_UNLOADED;
                                bool nearby = Vector3.Distance(horde.GetLocation(), otherHorde.GetLocation()) <= mergeDistance;
                                bool mergeChance = GameManager.Instance.World.GetGameRandom().RandomFloat >= 0.9f; // TODO: Calculate based on horde variables.

                                if (nearby && mergeChance)
                                {
                                    if (horde.Merge(otherHorde))
                                    {
                                        toRemove.Enqueue(otherHorde);
                                    }
                                    else if(otherHorde.Merge(horde))
                                    {
                                        toRemove.Enqueue(horde);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            });

            // Submit spawn requests.
            while(this.clusterSpawnRequests.TryDequeue(out HordeClusterSpawnRequest request))
            {
                this.mainThreadRequestProcessor.Request(request);
            }

            return eventReports.Count;
        }

        public void Add(WorldHorde horde)
        {
            toAdd.Enqueue(horde);
        }
    }
}