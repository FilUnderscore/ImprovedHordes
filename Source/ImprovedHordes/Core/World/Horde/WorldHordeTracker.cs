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
using ImprovedHordes.Core.World.Horde.Cluster;
using ImprovedHordes.Core.World.Horde.Spawn;
using ImprovedHordes.Core.World.Horde.Spawn.Request;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Core.World.Horde
{
    public readonly struct PlayerSnapshot
    {
        public readonly EntityPlayer player;
        public readonly Vector3 location;

        public PlayerSnapshot(EntityPlayer player, Vector3 location)
        {
            this.player = player;
            this.location = location;
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

    public sealed class WorldPlayerTracker : MainThreaded
    {
        private ThreadSubscription<List<PlayerSnapshot>> snapshots;

        public WorldPlayerTracker()
        {
            this.snapshots = new ThreadSubscription<List<PlayerSnapshot>>();
        }

        public ThreadSubscriber<List<PlayerSnapshot>> Subscribe()
        {
            return this.snapshots.Subscribe();
        }

        protected override void Shutdown()
        {
        }

        protected override void Update(float dt)
        {
            List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();

            foreach (var player in GameManager.Instance.World.Players.list)
            {
                snapshots.Add(new PlayerSnapshot(player, player.position));
            }

            this.snapshots.Update(snapshots);
        }
    }

    public sealed class WorldHordeTracker : Threaded, IData
    {
        private readonly Setting<int> MERGE_DISTANCE_LOADED = new Setting<int>("loaded_merge_distance", 10);
        private readonly Setting<int> MERGE_DISTANCE_UNLOADED = new Setting<int>("unloaded_merge_distance", 100);

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

        private readonly WorldPlayerTracker playerTracker;
        private readonly ThreadSubscriber<List<PlayerSnapshot>> snapshots;

        private readonly List<WorldEventReportEvent> eventsToReport = new List<WorldEventReportEvent>();

        private readonly Dictionary<Type, List<ClusterSnapshot>> clusterSnapshotsDict = new Dictionary<Type, List<ClusterSnapshot>>();
        private readonly ThreadSubscription<Dictionary<Type, List<ClusterSnapshot>>> clusterSnapshots = new ThreadSubscription<Dictionary<Type, List<ClusterSnapshot>>>();

        private WorldHordeSpawner spawner;

        private bool flushFlag = false;

        public WorldHordeTracker(ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory, IEntitySpawner entitySpawner, MainThreadRequestProcessor mainThreadRequestProcessor, WorldEventReporter reporter) : base(loggerFactory)
        {
            this.playerTracker = new WorldPlayerTracker();
            this.snapshots = this.playerTracker.Subscribe();

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

        private void UpdateHordesList()
        {
            // Add hordes.
            while (toAdd.TryDequeue(out WorldHorde cluster))
            {
                hordes.Add(cluster);
            }

            if (this.flushFlag)
            {
                foreach (var horde in this.hordes)
                    toRemove.Enqueue(horde);

                this.flushFlag = false;
            }

            // Remove dead/merged hordes.
            while (toRemove.TryDequeue(out WorldHorde cluster))
            {
                cluster.Cleanup(this.randomFactory);
                hordes.Remove(cluster);
            }
        }

        private void UpdateClusterSnapshots()
        {
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

        protected override void UpdateAsync(float dt)
        {
            this.UpdateHordesList();

            if (!this.snapshots.TryGet(out var snapshots))
                return;

            this.UpdateClusterSnapshots();
            
            int eventsProcessed = UpdateTrackerAsync(snapshots, this.eventsToReport.ToList(), dt);

            if (eventsProcessed > 0)
                this.eventsToReport.RemoveRange(0, eventsProcessed);
        }

        public WorldPlayerTracker GetPlayerTracker()
        {
            return this.playerTracker;
        }

        public ThreadSubscription<Dictionary<Type, List<ClusterSnapshot>>> GetClustersSubscription()
        {
            return this.clusterSnapshots;
        }

        public void SetHordeSpawner(WorldHordeSpawner spawner)
        {
            this.spawner = spawner;
        }

        private bool TryGetPlayerHordeGroupNear(List<PlayerHordeGroup> playerGroups, WorldHorde horde, out PlayerHordeGroup playerGroupNearby)
        {
            IEnumerable<PlayerHordeGroup> nearby = playerGroups.Where(playerGroup =>
            {
                float distance = horde.IsSpawned() ? MAX_VIEW_DISTANCE : MAX_VIEW_DISTANCE - 20;
                Vector3 hordeLocation = horde.GetLocation();

                foreach(var player in playerGroup.GetPlayers())
                {
                    if (Vector3.Distance(player.location, hordeLocation) <= distance)
                        return true;
                }

                return false;
            });

            if(!nearby.Any())
            {
                playerGroupNearby = default(PlayerHordeGroup);
                return false;
            }

            playerGroupNearby = nearby.First();
            return true;
        }

        private void TrySpawnHorde(WorldHorde horde, PlayerHordeGroup playerHordeGroup)
        {
            horde.RequestSpawns(this.spawner, playerHordeGroup, mainThreadRequestProcessor, this.randomFactory.GetSharedRandom(), entity =>
            {
                if (entity != null)
                    entitiesTracked.Add(entity.GetEntityId());
                else
                    this.Logger.Warn("Failed to track horde entity when spawning.");
            });
        }

        private void TrySpawnCluster(HordeCluster cluster, WorldHorde horde, PlayerHordeGroup playerHordeGroup)
        {
            if (cluster.TryGetSpawnRequest(out var spawnRequest))
            {
                if (spawnRequest.State.TryGet(out var spawnState))
                {
                    if (spawnState.complete && spawnState.spawned == 0) // Failed to spawn, despawn the horde and try again.
                    {
                        this.Logger.Warn("Failed to spawn horde cluster, retrying.");

                        cluster.SetSpawnState(Cluster.HordeCluster.SpawnState.DESPAWNED);
                        horde.RequestSpawn(cluster, this.spawner, playerHordeGroup, this.mainThreadRequestProcessor, this.randomFactory.GetSharedRandom(), entity =>
                        {
                            if (entity != null)
                                entitiesTracked.Add(entity.GetEntityId());
                            else
                                this.Logger.Warn("Failed to track horde entity when spawning.");
                        });

                        return;
                    }
                }
            }
        }

        private void UpdateHordeClusterEntity(HordeClusterEntity entity, PlayerHordeGroup playerHordeGroup)
        {
            if (!entity.IsAwaitingSpawnStateChange())
            {
                IEnumerable<PlayerSnapshot> nearby = playerHordeGroup.GetPlayers().Where(player =>
                {
                    float distance = entity.IsSpawned() ? MAX_VIEW_DISTANCE : MAX_VIEW_DISTANCE - 20;
                    return Vector3.Distance(player.location, entity.GetLocation()) <= distance;
                });

                entity.SetPlayersNearby(nearby.ToList());
                
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

        private void UpdateHorde(WorldHorde horde, float dt, List<PlayerHordeGroup> playerGroups, List<WorldEventReportEvent> eventReports)
        {
            if (TryGetPlayerHordeGroupNear(playerGroups, horde, out PlayerHordeGroup playerHordeGroup))
            {
                if (!horde.IsSpawned())
                {
                    TrySpawnHorde(horde, playerHordeGroup);
                }
                else
                {
                    foreach(var cluster in horde.GetClusters())
                    {
                        TrySpawnCluster(cluster, horde, playerHordeGroup);

                        foreach (var entity in cluster.GetEntities())
                        {
                            UpdateHordeClusterEntity(entity, playerHordeGroup);
                        }
                    }
                }
            }
            else if(horde.IsSpawned())
            {
                horde.Despawn(this.LoggerFactory, this.mainThreadRequestProcessor);
            }

            if (horde.IsSpawned())
            {
                horde.UpdatePosition(this.mainThreadRequestProcessor);
            }
            else
            {
                horde.UpdateDecay(dt);
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
                        horde.Interrupt(new GoToTargetAICommand(nearbyEvent.GetLocation()), new WanderAICommand(nearbyEvent.GetInterest()));
                    }
                }

                horde.Update(dt);
            }
        }

        private int UpdateTrackerAsync(List<PlayerSnapshot> players, List<WorldEventReportEvent> eventReports, float dt)
        {
            List<PlayerHordeGroup> playerHordeGroups = new List<PlayerHordeGroup>();

            // Assemble player horde groups from snapshots.
            for(int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                Vector2 playerLocation = new Vector2(player.location.x, player.location.z);

                PlayerHordeGroup playerGroup = new PlayerHordeGroup(player);

                for(int j = i + 1; j < players.Count; j++)
                {
                    var other = players[j];
                    Vector2 otherLocation = new Vector2(other.location.x, other.location.z);

                    if(Vector2.Distance(playerLocation, otherLocation) <= MAX_VIEW_DISTANCE)
                    {
                        playerGroup.AddPlayer(other);
                    }
                }

                playerHordeGroups.Add(playerGroup);
            }

            foreach(var horde in this.hordes)
            {
                UpdateHorde(horde, dt, playerHordeGroups, eventReports);
            }

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

        public void Flush()
        {
            this.flushFlag = true;
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