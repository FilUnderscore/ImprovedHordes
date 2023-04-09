using HarmonyLib;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde;
using ImprovedHordes.Source.Horde.AI;
using ImprovedHordes.Source.Horde.AI.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Thread = ImprovedHordes.Source.Utils.Thread;

namespace ImprovedHordes.Source.Core.Horde.World
{
    /**
     * Splits and merges horde clusters through tracking.
     */
    public sealed partial class WorldHordeClusterTracker
    {
        private readonly WorldHordeManager manager;
        private readonly AIExecutor aiExecutor;

        private readonly HordeClusterLoader<LoadedHordeCluster> hordeClusterLoader;
        private readonly HordeClusterLoader<UnloadedHordeCluster> hordeClusterUnloader;
        private readonly PlayerTracker playerTracker;

        // Shared
        private readonly List<PlayerSnapshot> Snapshots = new List<PlayerSnapshot>();
        private readonly LockedList<HordeCluster> Hordes = new LockedList<HordeCluster>();

        public WorldHordeClusterTracker(WorldHordeManager manager, AIExecutor aiExecutor)
        {
            this.manager = manager;
            this.aiExecutor = aiExecutor;

            this.hordeClusterLoader = new HordeClusterLoaderLoaded(this);
            this.hordeClusterUnloader = new HordeClusterLoaderUnloaded(this);
            this.playerTracker = new PlayerTracker(this, this.hordeClusterLoader, this.hordeClusterUnloader);

            this.playerTracker.ExecuteThread();
            this.hordeClusterLoader.ExecuteThread();
            this.hordeClusterUnloader.ExecuteThread();
        }

        /// <summary>
        /// Attempt to take a snapshot of all players if there are no snapshots waiting to be processed.
        /// </summary>
        private void TakeSnapshot()
        {
            if(Monitor.TryEnter(this.Snapshots))
            {
                if(this.Snapshots.Count == 0)
                {
                    foreach(var player in GameManager.Instance.World.Players.list)
                    {
                        if (player == null)
                            continue;

                        this.Snapshots.Add(new PlayerSnapshot(player));
                    }

                    this.playerTracker.Notify();
                }

                Monitor.Exit(this.Snapshots);
            }
        }

        public void Update()
        {
            this.TakeSnapshot();
        }

        /* in update()
        while(this.queuedClusters.TryDequeue(out IHordeCluster cluster)) 
        {
            hordes.Add(cluster);
        }

        for(int i = 0; i < hordes.Count; i++) 
        {
            IHordeCluster horde = hordes[i];

            if (i < hordes.Count - 1)
            {
                IHordeCluster otherHorde = hordes[i + 1];

                if (CanMerge(horde, otherHorde))
                {
                    horde.Recombine(otherHorde);
                    hordes.RemoveAt(i--);
                    continue;
                }
            }
        }
        */

        /*
        private bool CanMerge(IHordeCluster horde, IHordeCluster other)
        {
            float distance = Vector3.Distance(horde.GetLocation(), other.GetLocation());

            if (distance > 20.0f)
                return false;

            // See if they are

            return true;
        }
        */

        public void NotifyHordeClustersNearby(Vector3 location, float distance)
        {
            this.Hordes.StartWrite();
            
            this.Hordes.GetList().Where(hordeCluster => Vector3.Distance(hordeCluster.GetLocation(), location) <= distance).ToList().Do(cluster =>
            {
                NotifyCluster(cluster, location, distance);
            });

            this.Hordes.EndWrite();
        }

        private void NotifyCluster(HordeCluster cluster, Vector3 location, float interestLevel)
        {
            float densityToSplitAndDirect = (int)(cluster.GetEntityDensity() * (interestLevel / 100.0f));

            HordeCluster splitCluster;
            if (densityToSplitAndDirect < cluster.GetEntityDensity())
            {
                splitCluster = cluster.Split(densityToSplitAndDirect);
                this.Hordes.Add(splitCluster);
            }
            else
            {
                splitCluster = cluster;
            }

            foreach(IAIAgent agent in splitCluster.GetAIAgents())
            {
                this.aiExecutor.Queue(agent, new GoToTargetAICommand(location), true);
            }
        }

        /// <summary>
        /// Publicly accessible horde spawning mechanism.
        /// </summary>
        /// <param name="horde"></param>
        /// <param name="location"></param>
        /// <param name="num"></param>
        public void AddHorde(IHorde horde, Vector3 location, float density)
        {
            this.AddHorde(new UnloadedHordeCluster(this.manager.GetSpawner(), horde, location, density));
        }

        private void AddHorde(HordeCluster horde)
        {
            this.Hordes.StartWrite();
            this.Hordes.Add(horde);
            this.Hordes.EndWrite();

            Log.Out("Written");
        }

        public void Shutdown()
        {
            this.playerTracker.ShutdownThread();
            this.hordeClusterLoader.ShutdownThread();
            this.hordeClusterUnloader.ShutdownThread();
        }
    }
}