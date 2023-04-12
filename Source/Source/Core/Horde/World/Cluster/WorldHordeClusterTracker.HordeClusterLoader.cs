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