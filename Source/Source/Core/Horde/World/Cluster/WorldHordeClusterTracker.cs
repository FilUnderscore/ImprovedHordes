using HarmonyLib;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.Horde;
using ImprovedHordes.Source.Horde.AI;
using ImprovedHordes.Source.Horde.AI.Commands;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        private readonly LockedList<PlayerSnapshot> Snapshots = new LockedList<PlayerSnapshot>();        
        private readonly LockedList<int> EntitiesKilled = new LockedList<int>();

        // Personal
        private readonly List<int> entitiesKilled = new List<int>();

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
            using(var snapshotWriter = this.Snapshots.Set(false))
            {
                if(!snapshotWriter.IsWriting())
                    return;

                if (snapshotWriter.GetCount() == 0)
                {
                    foreach (var player in GameManager.Instance.World.Players.list)
                    {
                        if (player == null)
                            continue;

                        snapshotWriter.Add(new PlayerSnapshot(player));
                    }
                }
            }
        }

        private void ReportEntitiesKilled()
        {
            using(var entitiesKilledWriter = this.EntitiesKilled.Set(false))
            {
                if (!entitiesKilledWriter.IsWriting())
                    return;

                entitiesKilledWriter.AddRange(this.entitiesKilled);
                this.entitiesKilled.Clear();
            }
        }

        public void NotifyKilled(int entityId)
        {
            this.entitiesKilled.Add(entityId);
        }

        public void Update()
        {
            this.TakeSnapshot();
            this.ReportEntitiesKilled();
        }

        private bool AreClustersNearbyToMerge(HordeCluster cluster1, HordeCluster cluster2)
        {
            return Vector3.Distance(cluster1.GetLocation(), cluster2.GetLocation()) <= 20;
        }

        public void NotifyHordeClustersNearby(Vector3 location, float distance, float interestLevel)
        {
            Log.Out("notifying nearby : " + location + " and " + distance);
            
            using(var hordesWriter = this.Hordes.Set(true))
            {
                if (!hordesWriter.IsWriting())
                    return;

                hordesWriter.Get().Where(hordeCluster => Vector3.Distance(hordeCluster.GetLocation(), location) <= distance).ToList().Do(cluster =>
                {
                    NotifyCluster(hordesWriter, cluster, location, interestLevel);
                });
            }
        }

        private void NotifyCluster(LockedListWriter<HordeCluster> writer, HordeCluster cluster, Vector3 location, float interestLevel)
        {
            float interestLevel01 = (interestLevel / 100.0f);
            float chanceToSplit = 1.0f - interestLevel01;
            HordeCluster splitCluster;

            Log.Out("Chance to split: " + chanceToSplit + " Interest level: " + interestLevel01);
            if (GameManager.Instance.World.GetGameRandom().RandomFloat <= chanceToSplit)
            {
                float densityToSplitAndDirect = cluster.GetEntityDensity() * interestLevel01;

                if (densityToSplitAndDirect < cluster.GetEntityDensity())
                {
                    Log.Out("Split density: " + densityToSplitAndDirect);

                    splitCluster = cluster.Split(densityToSplitAndDirect);
                    writer.Add(splitCluster);
                }
                else
                {
                    splitCluster = cluster;
                }
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
            using(var hordesWriter = this.Hordes.Set(true))
            {
                if (!hordesWriter.IsWriting())
                    return;

                hordesWriter.Add(horde);
            }

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