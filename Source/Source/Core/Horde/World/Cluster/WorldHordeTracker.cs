using HarmonyLib;
using ImprovedHordes.Source.Core.Horde.World.Cluster.Characteristics;
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

        public static int MAX_VIEW_DISTANCE
        {
            get
            {
                return GameStats.GetInt(EnumGameStats.AllowedViewDistance) * 16;
            }
        }

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

        private readonly ConcurrentQueue<WorldEventReportEvent> eventsToReport = new ConcurrentQueue<WorldEventReportEvent>();

        private readonly ConcurrentQueue<HordeClusterSpawnRequest> clusterSpawnRequests = new ConcurrentQueue<HordeClusterSpawnRequest>();

        // Personal (main-thread), updated after task is completed.
        private readonly List<WorldHorde> hordes = new List<WorldHorde>();

        private readonly List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();
        private readonly List<WorldEventReportEvent> events = new List<WorldEventReportEvent>();

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
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach(var hordeType in types)
            {
                clusterSnapshots.Add(hordeType, new List<ClusterSnapshot>());
            }
        }

        private void Reporter_OnWorldEventReport(object sender, WorldEventReportEvent e)
        {
            Log.Out($"Pos {e.GetLocation()} Interest {e.GetInterest()} Dist {e.GetDistance()}");
            this.eventsToReport.Enqueue(e);
        }


        protected override void BeforeTaskRestart()
        {
            foreach (var player in GameManager.Instance.World.Players.list)
            {
                snapshots.Add(new PlayerSnapshot(player.position, player.gameStage, player.biomeStandingOn));
            }

            while(eventsToReport.TryDequeue(out var reportedEvent))
            {
                events.Add(reportedEvent);
            }
        }

        protected override void OnTaskFinish(int returnValue)
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
                this.events.RemoveRange(0, eventsProcessed);

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

        protected override int UpdateAsync(float dt)
        {
            return UpdateTrackerAsync(snapshots, this.events, dt);
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

        private int UpdateTrackerAsync(List<PlayerSnapshot> players, List<WorldEventReportEvent> eventReports, float dt)
        {
            Parallel.ForEach(this.hordes, horde =>
            {
                if(!horde.IsSpawned())
                {
                    IEnumerable<PlayerSnapshot> nearby = players.Where(player =>
                    {
                        float distance = horde.IsSpawned() ? MAX_VIEW_DISTANCE : MAX_VIEW_DISTANCE - 20;
                        return Vector3.Distance(player.location, horde.GetLocation()) <= distance;
                    });

                    if (nearby.Any() && (!horde.IsSpawned() || horde.HasClusterSpawnsWaiting()))
                    {
                        Log.Out("Spawn start");
                        PlayerHordeGroup group = new PlayerHordeGroup();
                        nearby.Do(player => group.AddPlayer(player.gamestage, player.biome));

                        foreach (var spawnRequest in horde.RequestSpawns(group))
                        {
                            this.clusterSpawnRequests.Enqueue(spawnRequest);
                        }
                        Log.Out("Spawn end");
                    }
                }
                else
                {
                    bool anyNearby = false;

                    Parallel.ForEach(horde.GetClusters(), cluster =>
                    {
                        foreach (var entity in cluster.GetEntities())
                        {
                            if (!entity.IsAwaitingSpawnStateChange())
                            {
                                IEnumerable<PlayerSnapshot> nearby = players.Where(player =>
                                {
                                    float distance = entity.IsSpawned() ? MAX_VIEW_DISTANCE : MAX_VIEW_DISTANCE - 20;
                                    return Vector3.Distance(player.location, entity.GetLocation()) <= distance;
                                });

                                anyNearby |= nearby.Any();

                                if (entity.IsSpawned() && !nearby.Any())
                                {
                                    entity.RequestDespawn(this.mainThreadRequestProcessor);
                                }
                                else if (!entity.IsSpawned() && nearby.Any())
                                {
                                    entity.RequestSpawn(this.mainThreadRequestProcessor);
                                }
                            }
                        }
                    });

                    if (!anyNearby)
                        horde.Despawn(this.mainThreadRequestProcessor);
                }

                if (horde.IsSpawned())
                {
                    horde.UpdatePosition(this.mainThreadRequestProcessor);
                }

                if (horde.IsDead())
                {
                    toRemove.Enqueue(horde);
                }
                else
                {
                    // Tick AI.

                    HordeCharacteristics characteristics = horde.GetCharacteristics();

                    if (characteristics.HasCharacteristic<SensitivityHordeCharacteristic>())
                    {
                        float sensitivity = characteristics.GetCharacteristic<SensitivityHordeCharacteristic>().GetSensitivity();

                        IEnumerable<WorldEventReportEvent> nearbyReports = eventReports.Where(report =>
                        {
                            return Vector3.Distance(report.GetLocation(), horde.GetLocation()) <= report.GetDistance() * sensitivity;
                        });

                        if (nearbyReports.Any())
                        {
                            // Interrupt AI to split off/target reported event.
                            WorldEventReportEvent nearbyEvent = nearbyReports.OrderBy(report => report.GetDistance()).First();
                            horde.Interrupt(new GoToTargetAICommand(nearbyEvent.GetLocation()));
                        }
                    }

                    horde.Update(dt);
                }
            });

            // Merge nearby hordes.
            for (int index = 0; index < this.hordes.Count; index++)
            {
                WorldHorde horde = this.hordes[index];

                if (!horde.IsDead())
                {
                    for (int j = index + 1; j < this.hordes.Count; j++)
                    {
                        WorldHorde otherHorde = this.hordes[j];

                        if (!otherHorde.IsDead())
                        {
                            int mergeDistance = horde.IsSpawned() ? MERGE_DISTANCE_LOADED : MERGE_DISTANCE_UNLOADED;

                            bool nearby = Vector3.Distance(horde.GetLocation(), otherHorde.GetLocation()) <= mergeDistance;
                            bool mergeChance = this.Random.RandomFloat >= 0.9f; // TODO: Calculate based on horde variables.

                            if (nearby && mergeChance)
                            {
                                if (horde.Merge(otherHorde))
                                {
                                    toRemove.Enqueue(otherHorde);
                                }
                                else if (otherHorde.Merge(horde))
                                {
                                    toRemove.Enqueue(horde);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // Submit spawn requests.
            while (this.clusterSpawnRequests.TryDequeue(out HordeClusterSpawnRequest request))
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