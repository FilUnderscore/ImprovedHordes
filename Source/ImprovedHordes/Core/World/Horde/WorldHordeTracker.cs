using ConcurrentCollections;
using HarmonyLib;
using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Event;
using ImprovedHordes.Core.World.Horde.AI.Commands;
using ImprovedHordes.Core.World.Horde.Characteristics;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.Core.World.Horde.Spawn.Request;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde
{
    public sealed class WorldHordeTracker : MainThreadSynchronizedTask<int>, IData
    {
        private readonly Setting<int> MERGE_DISTANCE_LOADED = new Setting<int>("loaded_merge_distance", 10);
        private readonly Setting<int> MERGE_DISTANCE_UNLOADED = new Setting<int>("unloaded_merge_distance", 100);

        private readonly Setting<int> HORDE_THREADS = new Setting<int>("max_horde_threads", 4);
        private readonly Setting<int> HORDE_CLUSTER_THREADS = new Setting<int>("max_cluster_threads", 2);

        public static readonly Setting<float> MAX_HORDE_DENSITY = new Setting<float>("max_horde_density", 2.0f);
        public static readonly Setting<float> MAX_WORLD_DENSITY = new Setting<float>("max_world_density", 500.0f);

        public static readonly Setting<int> MAX_ENTITIES_SPAWNED_PER_PLAYER = new Setting<int>("max_entities_spawned_per_player", 16);

        public static int MAX_VIEW_DISTANCE
        {
            get
            {
                return GameStats.GetInt(EnumGameStats.AllowedViewDistance) * 16;
            }
        }

        private ParallelOptions ParallelHordeOptions;
        private ParallelOptions ParallelClusterOptions;

        public readonly struct PlayerSnapshot
        {
            public readonly EntityPlayer player;
            public readonly Vector3 location;
            public readonly int gamestage;
            public readonly string biome;

            public PlayerSnapshot(EntityPlayer player, Vector3 location, int gamestage, BiomeDefinition biome)
            {
                this.player = player;
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

        private readonly IRandomFactory<IWorldRandom> randomFactory;
        private readonly IEntitySpawner entitySpawner;
        private readonly MainThreadRequestProcessor mainThreadRequestProcessor;
        
        // Shared
        private readonly ConcurrentQueue<WorldHorde> toAdd = new ConcurrentQueue<WorldHorde>();
        private readonly ConcurrentQueue<WorldHorde> toRemove = new ConcurrentQueue<WorldHorde>();

        private readonly ConcurrentQueue<HordeClusterSpawnMainThreadRequest> clusterSpawnRequests = new ConcurrentQueue<HordeClusterSpawnMainThreadRequest>();
        private readonly ConcurrentHashSet<int> entitiesTracked = new ConcurrentHashSet<int>();

        // Personal (main-thread), updated after task is completed.
        private readonly List<WorldHorde> hordes = new List<WorldHorde>();

        private readonly List<PlayerSnapshot> snapshotsList = new List<PlayerSnapshot>();
        private readonly ThreadSubscription<List<PlayerSnapshot>> snapshots = new ThreadSubscription<List<PlayerSnapshot>>();

        private readonly List<WorldEventReportEvent> eventsToReport = new List<WorldEventReportEvent>();

        private readonly Dictionary<Type, List<ClusterSnapshot>> clusterSnapshotsDict = new Dictionary<Type, List<ClusterSnapshot>>();
        private readonly ThreadSubscription<Dictionary<Type, List<ClusterSnapshot>>> clusterSnapshots = new ThreadSubscription<Dictionary<Type, List<ClusterSnapshot>>>();

        private WorldHordeSpawner spawner;

        public WorldHordeTracker(ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory, IEntitySpawner entitySpawner, MainThreadRequestProcessor mainThreadRequestProcessor, WorldEventReporter reporter) : base(loggerFactory)
        {
            this.randomFactory = randomFactory;
            this.entitySpawner = entitySpawner;
            this.mainThreadRequestProcessor = mainThreadRequestProcessor;

            reporter.OnWorldEventReport += Reporter_OnWorldEventReport;

            this.RegisterHordes();
            EntityAlive_canDespawn_Patch.Tracker = this;

            HORDE_THREADS.OnSettingUpdated += HORDE_THREADS_OnSettingUpdated;
            HORDE_CLUSTER_THREADS.OnSettingUpdated += HORDE_CLUSTER_THREADS_OnSettingUpdated;
        }

        private void HORDE_THREADS_OnSettingUpdated(object sender, EventArgs e)
        {
            ParallelHordeOptions = new ParallelOptions { MaxDegreeOfParallelism = HORDE_THREADS.Value };
        }

        private void HORDE_CLUSTER_THREADS_OnSettingUpdated(object sender, EventArgs e)
        {
            ParallelClusterOptions = new ParallelOptions { MaxDegreeOfParallelism = HORDE_CLUSTER_THREADS.Value };
        }

        private void RegisterHordes()
        {
            var type = typeof(IHorde);
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach(var hordeType in types)
            {
                clusterSnapshotsDict.Add(hordeType, new List<ClusterSnapshot>());
            }
        }

        private void Reporter_OnWorldEventReport(object sender, WorldEventReportEvent e)
        {
            this.Logger.Verbose($"World Event Reported: Pos {e.GetLocation()} Location Interest {e.GetInterest()} Location Interest Distance {e.GetDistance()}");

            this.eventsToReport.Add(e);
        }

        protected override void BeforeTaskRestart()
        {
            foreach (var player in GameManager.Instance.World.Players.list)
            {
                snapshotsList.Add(new PlayerSnapshot(player, player.position, player.gameStage, player.biomeStandingOn));
            }

            snapshots.Update(this.snapshotsList.ToList());
        }

        protected override void OnTaskFinish(int returnValue)
        {
            // Clear old snapshots after task is complete.
            snapshotsList.Clear();

            // Add hordes.
            while (toAdd.TryDequeue(out WorldHorde cluster))
            {
                hordes.Add(cluster);
            }

            // Remove dead/merged hordes.
            while (toRemove.TryDequeue(out WorldHorde cluster))
            {
                cluster.Cleanup(this.randomFactory);
                hordes.Remove(cluster);
            }

            int eventsProcessed = returnValue;

            if (eventsProcessed > 0)
                this.eventsToReport.RemoveRange(0, eventsProcessed);

            // Update cluster snapshots and remove outdated ones.

            foreach (var key in clusterSnapshotsDict.Keys)
            {
                clusterSnapshotsDict[key].Clear();
            }

            foreach (var horde in this.hordes)
            {
                foreach (var cluster in horde.GetClusters())
                {
                    clusterSnapshotsDict[cluster.GetHorde().GetType()].Add(new ClusterSnapshot(cluster.GetHorde(), horde.GetLocation(), cluster.GetDensity()));
                }
            }

            clusterSnapshots.Update(this.clusterSnapshotsDict.ToDictionary(k => k.Key, v => v.Value.ToList()));
        }

        protected override int UpdateAsync(float dt)
        {
            return UpdateTrackerAsync(snapshotsList, this.eventsToReport.ToList(), dt);
        }

        public ThreadSubscription<Dictionary<Type, List<ClusterSnapshot>>> GetClustersSubscription()
        {
            return this.clusterSnapshots;
        }

        public ThreadSubscription<List<PlayerSnapshot>> GetPlayersSubscription()
        {
            return this.snapshots;
        }

        public void SetHordeSpawner(WorldHordeSpawner spawner)
        {
            this.spawner = spawner;
        }

        private PlayerHordeGroup GetPlayerHordeGroupNear(List<PlayerSnapshot> players, WorldHorde horde)
        {
            IEnumerable<PlayerSnapshot> nearby = players.Where(player =>
            {
                float distance = horde.IsSpawned() ? MAX_VIEW_DISTANCE : MAX_VIEW_DISTANCE - 20;
                return Vector3.Distance(player.location, horde.GetLocation()) <= distance;
            });

            if (nearby.Any())
            {
                PlayerHordeGroup group = new PlayerHordeGroup();
                nearby.Do(player => group.AddPlayer(player.player, player.gamestage, player.biome));

                return group;
            }

            return null;
        }

        private void UpdateHorde(WorldHorde horde, float dt, List<PlayerSnapshot> players, List<WorldEventReportEvent> eventReports)
        {
            if (!horde.IsSpawned())
            {
                PlayerHordeGroup playerHordeGroup = GetPlayerHordeGroupNear(players, horde);

                if (playerHordeGroup != null)
                {
                    horde.RequestSpawns(this.spawner, playerHordeGroup, mainThreadRequestProcessor, this.randomFactory.GetSharedRandom(), entity =>
                    {
                        if (entity != null)
                            entitiesTracked.Add(entity.GetEntityId());
                        else
                            this.Logger.Warn("Failed to track horde entity when spawning.");
                    });
                }
            }
            else
            {
                bool anyNearby = false;

                Parallel.ForEach(horde.GetClusters(), ParallelClusterOptions, cluster =>
                {
                    if (cluster.TryGetSpawnRequest(out var spawnRequest))
                    {
                        if (spawnRequest.State.TryGet(out var spawnState))
                        {
                            if (spawnState.complete && spawnState.spawned == 0) // Failed to spawn, despawn the horde and try again.
                            {
                                this.Logger.Warn("Failed to spawn horde cluster, retrying.");

                                PlayerHordeGroup playerHordeGroup = GetPlayerHordeGroupNear(players, horde);

                                if (playerHordeGroup != null)
                                {
                                    cluster.SetSpawnState(Cluster.HordeCluster.SpawnState.DESPAWNED);
                                    horde.RequestSpawn(cluster, this.spawner, playerHordeGroup, this.mainThreadRequestProcessor, this.randomFactory.GetSharedRandom(), entity =>
                                    {
                                        if (entity != null)
                                            entitiesTracked.Add(entity.GetEntityId());
                                        else
                                            this.Logger.Warn("Failed to track horde entity when spawning.");
                                    });
                                }

                                return;
                            }
                        }
                    }

                    foreach (var entity in cluster.GetEntities())
                    {
                        if (!entity.IsAwaitingSpawnStateChange())
                        {
                            IEnumerable<PlayerSnapshot> nearby = players.Where(player =>
                            {
                                float distance = entity.IsSpawned() ? MAX_VIEW_DISTANCE : MAX_VIEW_DISTANCE - 20;
                                return Vector3.Distance(player.location, entity.GetLocation()) <= distance;
                            });

                            entity.SetPlayersNearby(nearby);
                            anyNearby |= nearby.Any();

                            if (entity.IsSpawned() && !nearby.Any())
                            {
                                entity.RequestDespawn(this.LoggerFactory, this.mainThreadRequestProcessor, entityAlive =>
                                {
                                    if (entityAlive == null || !entitiesTracked.TryRemove(entityAlive.GetEntityId()))
                                        this.Logger.Warn("Failed to untrack horde entity when despawning.");
                                });
                            }
                            else if (!entity.IsSpawned() && nearby.Any())
                            {
                                entity.RequestSpawn(this.LoggerFactory, this.entitySpawner, this.mainThreadRequestProcessor, entityAlive =>
                                {
                                    if (entityAlive != null)
                                        entitiesTracked.Add(entityAlive.GetEntityId());
                                    else
                                        this.Logger.Warn("Failed to track horde entity when spawning.");
                                });
                            }
                        }
                    }
                });

                if (!anyNearby)
                    horde.Despawn(this.LoggerFactory, this.mainThreadRequestProcessor);
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
        }

        private int UpdateTrackerAsync(List<PlayerSnapshot> players, List<WorldEventReportEvent> eventReports, float dt)
        {
            Parallel.ForEach(this.hordes, ParallelHordeOptions, horde =>
            {
                UpdateHorde(horde, dt, players, eventReports);
            });

            // Merge nearby hordes.
            for (int index = 0; index < this.hordes.Count - 1; index++)
            {
                WorldHorde horde = this.hordes[index];

                if (!horde.IsDead())
                {
                    if (horde.Split(this.LoggerFactory, this.mainThreadRequestProcessor, out var newHordes))
                    {
                        foreach (var newHorde in newHordes)
                        {
                            this.Add(newHorde);
                        }
                    }
                    else
                    {
                        for (int j = index + 1; j < this.hordes.Count; j++)
                        {
                            WorldHorde otherHorde = this.hordes[j];

                            if (!otherHorde.IsDead())
                            {
                                int mergeDistance = horde.IsSpawned() ? MERGE_DISTANCE_LOADED.Value : MERGE_DISTANCE_UNLOADED.Value;
                                bool nearby = Vector3.Distance(horde.GetLocation(), otherHorde.GetLocation()) <= mergeDistance;

                                if (nearby)
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
            }

            // Submit spawn requests.
            while (this.clusterSpawnRequests.TryDequeue(out HordeClusterSpawnMainThreadRequest request))
            {
                this.mainThreadRequestProcessor.Request(request);
            }

            return eventReports.Count;
        }

        public void Add(WorldHorde horde)
        {
            toAdd.Enqueue(horde);
        }

        public IData Load(IDataLoader loader)
        {
            this.hordes.AddRange(loader.Load<List<WorldHorde>>());

            return this;
        }

        public void Save(IDataSaver saver)
        {
            saver.Save<List<WorldHorde>>(this.hordes);
        }

        [HarmonyPatch(typeof(EntityAlive))]
        [HarmonyPatch("canDespawn")]
        private sealed class EntityAlive_canDespawn_Patch
        {
            public static WorldHordeTracker Tracker;

            static bool Prefix(EntityAlive __instance, ref bool __result)
            {
                if (!Tracker.entitiesTracked.Contains(__instance.entityId))
                    return true;

                __result = false;
                return false;
            }
        }
    }
}