using HarmonyLib;
using ImprovedHordes.Source.Core.Horde.World.Spawn;
using ImprovedHordes.Source.Core.Threading;
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
            private readonly LockedList<HordeCluster> clustersToLoad = new LockedList<HordeCluster>();
            
            public HordeClusterLoader(WorldHordeClusterTracker tracker) : base("IH-" + typeof(T).Name + "Loader")
            {
                this.tracker = tracker;
            }

            public void Notify(List<HordeCluster> clusters)
            {
                using(var clustersToLoadWriter = clustersToLoad.Set(true))
                {
                    if (!clustersToLoadWriter.IsWriting())
                        return;

                    clustersToLoadWriter.AddRange(clusters);
                }
            }

            public abstract T Create(WorldHordeSpawner spawner, HordeCluster cluster);

            public override bool OnLoop()
            {
                using(var clustersToLoadWriter = clustersToLoad.Set(true))
                {
                    if (!clustersToLoadWriter.IsWriting())
                        return true;

                    if (clustersToLoadWriter.GetCount() > 0)
                    {
                        using (var hordesWriter = this.tracker.Hordes.Set(true))
                        {
                            if (!hordesWriter.IsWriting())
                                return true;

                            foreach (var cluster in clustersToLoadWriter)
                            {
                                if (!Monitor.TryEnter(cluster.Lock) || cluster is T || !hordesWriter.Contains(cluster))
                                    continue;

                                T loadedHordeCluster = Create(this.tracker.manager.GetSpawner(), cluster);
                                cluster.OnStateChange();

                                Log.Out($"From {cluster.GetType().FullName} to {typeof(T).FullName}");

                                loadedHordeCluster.GetAIAgents().Do(agent => this.tracker.aiExecutor.RegisterAgent(agent));
                                cluster.GetAIAgents().Do(agent => this.tracker.aiExecutor.UnregisterAgent(agent));

                                hordesWriter.Add(loadedHordeCluster);
                                hordesWriter.Remove(cluster);

                                Monitor.Exit(cluster.Lock);
                            }

                            Log.Out("Count: " + hordesWriter.GetCount());
                            clustersToLoadWriter.Clear();
                        }
                    }
                }

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