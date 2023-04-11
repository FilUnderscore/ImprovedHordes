using HarmonyLib;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Thread = ImprovedHordes.Source.Utils.Thread;

namespace ImprovedHordes.Source.Core.Horde.World
{
    public sealed partial class WorldHordeClusterTracker
    {
        private static bool IsNearby(Vector3 location, HordeCluster cluster)
        {
            int distance = 90;
            return Mathf.FloorToInt(Vector3.Distance(location, cluster.GetLocation())) <= distance;
        }

        private sealed class PlayerTracker : Thread
        {
            private readonly WorldHordeClusterTracker tracker;

            private readonly HordeClusterLoader<LoadedHordeCluster> loader;
            private readonly HordeClusterLoader<UnloadedHordeCluster> unloader;

            private readonly Dictionary<bool, List<HordeCluster>> clustersToChange = new Dictionary<bool, List<HordeCluster>>();
            private readonly List<HordeCluster> clustersToRemove = new List<HordeCluster>();

            public PlayerTracker(WorldHordeClusterTracker tracker, HordeClusterLoader<LoadedHordeCluster> loader, HordeClusterLoader<UnloadedHordeCluster> unloader) : base("IH-PlayerTracker")
            {
                this.tracker = tracker;
                this.loader = loader;
                this.unloader = unloader;

                this.clustersToChange.Add(true, new List<HordeCluster>());
                this.clustersToChange.Add(false, new List<HordeCluster>());
            }

            public string GetName()
            {
                return "IH-PlayerTracker";
            }

            public override bool OnLoop()
            {
                Monitor.Enter(this.tracker.SnapshotsLock);
                
                if (this.tracker.Snapshots.Count == 0)
                {
                    Monitor.Exit(this.tracker.SnapshotsLock);
                    return true;
                }    

                bool loaded = false, unloaded = false;
                if (this.tracker.Hordes.TryRead())
                {
                    // Begin read.
                    foreach (HordeCluster cluster in this.tracker.Hordes) // Iterate hordes first to reduce locks.
                    {
                        if (!Monitor.TryEnter(cluster.Lock) || cluster.NextStateSet())
                            continue;

                        bool anyNearby = IsPlayerNearby(cluster);

                        if (cluster.IsLoaded() != anyNearby)
                        {
                            cluster.SetNextStateSet(true);

                            if (!cluster.IsLoaded())
                                cluster.SetNearbyPlayerGroup(DeterminePlayerGroup(cluster));

                            clustersToChange[anyNearby].Add(cluster);

                            loaded |= anyNearby;
                            unloaded |= !anyNearby;
                        }
                        else if (cluster.IsLoaded())
                        {
                            ((LoadedHordeCluster)cluster).Notify(this.tracker.EntsKilled); 
                        }

                        if (cluster.GetEntityDensity() == 0.0f)
                        {
                            clustersToRemove.Add(cluster);
                        }

                        Monitor.Exit(cluster.Lock);
                    }

                    this.tracker.Hordes.EndRead();

                    this.tracker.Hordes.StartWrite();
                    // Begin Write.

                    foreach (var cluster in clustersToRemove)
                    {
                        Log.Out("Removed");
                        this.tracker.Hordes.Remove(cluster);
                    }

                    clustersToRemove.Clear();

                    this.tracker.Hordes.EndWrite();
                }

                this.tracker.Snapshots.Clear();
                this.tracker.EntsKilled.Clear();

                Monitor.Exit(this.tracker.SnapshotsLock);

                if(loaded)
                    this.loader.Notify(clustersToChange[true]);
    
                if(unloaded)
                    this.unloader.Notify(clustersToChange[false]);

                clustersToChange[true].Clear();
                clustersToChange[false].Clear();

                return true;
            }

            private bool IsPlayerNearby(HordeCluster cluster)
            {
                bool anyNearby = false;

                foreach (PlayerSnapshot snapshot in this.tracker.Snapshots)
                {
                    Vector3 location = snapshot.GetLocation();
                    int gamestage = snapshot.GetGamestage();
                    bool nearby = IsNearby(location, cluster);

                    anyNearby |= nearby;

                    if (nearby)
                        break;
                }

                return anyNearby;
            }

            private PlayerHordeGroup DeterminePlayerGroup(HordeCluster cluster)
            {
                PlayerHordeGroup playerGroup = new PlayerHordeGroup();

                foreach (PlayerSnapshot snapshot in this.tracker.Snapshots)
                {
                    Vector3 location = snapshot.GetLocation();
                    int gamestage = snapshot.GetGamestage();
                    bool nearby = IsNearby(location, cluster);

                    if (nearby)
                        playerGroup.AddPlayer(gamestage);
                }

                return playerGroup;
            }
        }

        /// <summary>
        /// Loads horde clusters.
        /// </summary>
        private abstract class HordeClusterLoader<T> : Thread where T: HordeCluster
        {
            private readonly WorldHordeClusterTracker tracker;

            private readonly List<HordeCluster> clustersToLoad = new List<HordeCluster>();
            private readonly object clustersToLoadLock = new object();

            public HordeClusterLoader(WorldHordeClusterTracker tracker) : base("IH-" + typeof(T).Name + "Loader")
            {
                this.tracker = tracker;
            }

            public void Notify(List<HordeCluster> clusters)
            {
                Monitor.Enter(this.clustersToLoadLock);
                clustersToLoad.AddRange(clusters);
                Monitor.Exit(this.clustersToLoadLock);
            }

            public abstract T Create(WorldHordeSpawner spawner, HordeCluster cluster);

            public override bool OnLoop()
            {
                Monitor.Enter(this.clustersToLoadLock);
                if (this.clustersToLoad.Count > 0)
                {
                    this.tracker.Hordes.StartWrite();

                    foreach (var cluster in this.clustersToLoad)
                    {
                        if (!Monitor.TryEnter(cluster.Lock) || cluster is T || !this.tracker.Hordes.Contains(cluster))
                            continue;

                        T loadedHordeCluster = Create(this.tracker.manager.GetSpawner(), cluster);
                        cluster.OnStateChange();

                        Log.Out($"From {cluster.GetType().FullName} to {typeof(T).FullName}");

                        loadedHordeCluster.GetAIAgents().Do(agent => this.tracker.aiExecutor.RegisterAgent(agent));
                        cluster.GetAIAgents().Do(agent => this.tracker.aiExecutor.UnregisterAgent(agent));

                        this.tracker.Hordes.Add(loadedHordeCluster);
                        this.tracker.Hordes.Remove(cluster);

                        Monitor.Exit(cluster.Lock);
                    }

                    Log.Out("Count: " + this.tracker.Hordes.GetCount());

                    this.tracker.Hordes.EndWrite();
                    this.clustersToLoad.Clear();
                }
                Monitor.Exit(this.clustersToLoadLock);

                return true;
            }
        }

        private sealed class HordeClusterLoaderLoaded : HordeClusterLoader<LoadedHordeCluster>
        {
            public HordeClusterLoaderLoaded(WorldHordeClusterTracker tracker) : base(tracker)
            {
            }

            public override LoadedHordeCluster Create(WorldHordeSpawner spawner, HordeCluster cluster)
            {
                if (!(cluster is UnloadedHordeCluster uhc))
                    throw new InvalidOperationException("Cannot convert loaded horde cluster to unloaded horde cluster.");

                return new LoadedHordeCluster(spawner, uhc);
            }
        }

        private sealed class HordeClusterLoaderUnloaded : HordeClusterLoader<UnloadedHordeCluster>
        {
            public HordeClusterLoaderUnloaded(WorldHordeClusterTracker tracker) : base(tracker)
            {
            }

            public override UnloadedHordeCluster Create(WorldHordeSpawner spawner, HordeCluster cluster)
            {
                if (!(cluster is LoadedHordeCluster lhc))
                    throw new InvalidOperationException("Cannot convert unloaded horde cluster to loaded horde cluster.");

                return new UnloadedHordeCluster(spawner, lhc);
            }
        }
    }
}