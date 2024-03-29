﻿using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.World.Horde.Spawn;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ImprovedHordes.Core.World.Horde.Populator
{
    public sealed class WorldHordePopulator : Threaded, IData
    {
        private readonly ThreadSubscriber<List<PlayerHordeGroup>> playerGroups;
        private readonly ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> clusters;

        private readonly WorldHordeSpawner spawner;
        private readonly float worldSize;

        private readonly List<HordePopulator> populators = new List<HordePopulator>();

        public WorldHordePopulator(ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory, WorldHordeTracker tracker, WorldHordeSpawner spawner, float worldSize) : base(loggerFactory, randomFactory)
        {
            this.spawner = spawner;
            this.worldSize = worldSize;

            this.playerGroups = tracker.GetPlayerTracker().Subscribe();
            this.clusters = tracker.GetClustersSubscription().Subscribe();
        }

        private float GetWorldHordeDensity(Dictionary<Type, List<ClusterSnapshot>> clusters)
        {
            float worldHordeDensity = 0.0f;

            foreach(var clusterList in clusters.Values)
            {
                float clusterTypeWorldHordeDensity = clusterList.Sum(cluster => cluster.density);
                worldHordeDensity += clusterTypeWorldHordeDensity;
            }

            return worldHordeDensity;
        }

        protected override void UpdateAsync(float dt)
        {
            if(!this.playerGroups.TryGet(out var playerGroups) || !this.clusters.TryGet(out var clusters))
                return;

            float worldSizeKM = worldSize / 1000;

            if (GetWorldHordeDensity(clusters) >= (WorldHordeTracker.DENSITY_PER_KM_SQUARED.Value * worldSizeKM * worldSizeKM))
                return;

            foreach(var populator in this.populators)
            {
                if (!populator.CanRun(playerGroups, clusters))
                    continue;

                populator.Populate(dt, playerGroups, clusters, this.spawner, this.Random);
            }
        }

        public void RegisterPopulator(HordePopulator populator)
        {
            this.populators.Add(populator);
        }

        public IData Load(IDataLoader loader)
        {
            foreach(var populator in this.populators)
            {
                populator.Load(loader);
            }

            return this;
        }

        public void Save(IDataSaver saver)
        {
            foreach(var populator in this.populators)
            {
                populator.Save(saver);
            }
        }

        public void Flush()
        {
            foreach(var populator in this.populators)
            {
                populator.Flush();
            }
        }
    }
}
