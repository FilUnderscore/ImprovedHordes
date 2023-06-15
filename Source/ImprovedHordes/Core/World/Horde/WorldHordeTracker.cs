using ConcurrentCollections;
using HarmonyLib;
using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
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
    public sealed class WorldHordeTracker : MainThreadSynchronizedTask<int>
    {
        private const int MERGE_DISTANCE_LOADED = 10;
        private const int MERGE_DISTANCE_UNLOADED = 100;

        private const int HORDE_THREADS = 4;
        private const int HORDE_CLUSTER_THREADS = 2;

        private const float MAX_HORDE_DENSITY = 10.0f;
        private const float MAX_WORLD_DENSITY = 160.0f;

        private const int MAX_ENTITIES_SPAWNED_PER_PLAYER = 20;

        public static int MAX_VIEW_DISTANCE
        {
            get
            {
                return GameStats.GetInt(EnumGameStats.AllowedViewDistance) * 16;
            }
        }

        private readonly ParallelOptions ParallelHordeOptions = new ParallelOptions { MaxDegreeOfParallelism = HORDE_THREADS };
        private readonly ParallelOptions ParallelClusterOptions = new ParallelOptions { MaxDegreeOfParallelism = HORDE_CLUSTER_THREADS };

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

        private int UpdateTrackerAsync(List<PlayerSnapshot> players, List<WorldEventReportEvent> eventReports, float dt)
        {
            Parallel.ForEach(this.hordes, ParallelHordeOptions, horde =>
            {
                if(!horde.IsSpawned())
                {
                    PlayerHordeGroup playerHordeGroup = GetPlayerHordeGroupNear(players, horde);
                    
                    if(playerHordeGroup != null)
                    {
                        horde.RequestSpawns(this.spawner, playerHordeGroup, mainThreadRequestProcessor, this.randomFactory.GetSharedRandom(), entity => entitiesTracked.Add(entity.GetEntityId()));
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
                                        cluster.SetSpawned(false);
                                        horde.RequestSpawn(cluster, this.spawner, playerHordeGroup, this.mainThreadRequestProcessor, this.randomFactory.GetSharedRandom(), entity => entitiesTracked.Add(entity.GetEntityId()));
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

                                anyNearby |= nearby.Any();

                                if (entity.IsSpawned() && !nearby.Any())
                                {
                                    entity.RequestDespawn(this.LoggerFactory, this.mainThreadRequestProcessor, entityAlive =>
                                    {
                                        if (!entitiesTracked.TryRemove(entityAlive.GetEntityId()))
                                            this.Logger.Warn("Failed to untrack horde entity when despawning.");
                                    });
                                }
                                else if (!entity.IsSpawned() && nearby.Any())
                                {
                                    entity.RequestSpawn(this.LoggerFactory, this.entitySpawner, this.mainThreadRequestProcessor, entityAlive => entitiesTracked.Add(entityAlive.GetEntityId()));
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
            });

            // Merge nearby hordes.
            for (int index = 0; index < this.hordes.Count - 1; index++)
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