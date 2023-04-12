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
		private sealed class PlayerTracker : Thread
		{
			private readonly WorldHordeClusterTracker tracker;

			private readonly HordeClusterLoader<LoadedHordeCluster> loader;
			private readonly HordeClusterLoader<UnloadedHordeCluster> unloader;

			private readonly Dictionary<bool, List<HordeCluster>> clustersToChange = new Dictionary<bool, List<HordeCluster>>();
			private readonly List<HordeCluster> clustersToRemove = new List<HordeCluster>();

			private readonly List<int> killed = new List<int>();

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
				using(var snapshotsWriter = this.tracker.Snapshots.Set(true))
				{
					if (!snapshotsWriter.IsWriting())
						return true;

					if (snapshotsWriter.GetCount() == 0)
						return true;

					using (var entitiesKilledWriter = this.tracker.EntitiesKilled.Set(true))
                    {
						if (entitiesKilledWriter.IsWriting())
						{
							killed.AddRange(entitiesKilledWriter.Get());
							entitiesKilledWriter.Clear();
						}
					}

                    bool loaded = false, unloaded = false;
					using (var hordesReader = this.tracker.Hordes.Get(false))
					{
                        if (hordesReader.IsReading())
                        {
                            // Begin read.
                            foreach (HordeCluster cluster in hordesReader) // Iterate hordes first to reduce locks.
                            {
                                if (!Monitor.TryEnter(cluster.Lock) || cluster.NextStateSet())
                                    continue;

                                bool anyNearby = IsPlayerNearby(snapshotsWriter, cluster);

                                if (cluster.IsLoaded() != anyNearby)
                                {
                                    cluster.SetNextStateSet(true);

                                    if (!cluster.IsLoaded())
                                        cluster.SetNearbyPlayerGroup(DeterminePlayerGroup(snapshotsWriter, cluster));

                                    clustersToChange[anyNearby].Add(cluster);

                                    loaded |= anyNearby;
                                    unloaded |= !anyNearby;
                                }
                                else if (cluster.IsLoaded())
                                {
                                    ((LoadedHordeCluster)cluster).Notify(killed);
                                }

                                if (cluster.GetEntityDensity() == 0.0f)
                                {
                                    clustersToRemove.Add(cluster);
                                }

                                Monitor.Exit(cluster.Lock);
                            }
                        }
                    }

                    snapshotsWriter.Clear();

                    using (var hordesWriter = this.tracker.Hordes.Set(true))
					{
                        foreach (var cluster in clustersToRemove)
                        {
                            Log.Out("Removed");
                            hordesWriter.Remove(cluster);
                        }

                        clustersToRemove.Clear();
                    }

                    if (loaded)
                        this.loader.Notify(clustersToChange[true]);

                    if (unloaded)
                        this.unloader.Notify(clustersToChange[false]);

                    clustersToChange[true].Clear();
                    clustersToChange[false].Clear();

					killed.Clear();
                }

				return true;
			}

			private bool IsPlayerNearby(LockedListWriter<PlayerSnapshot> snapshotsWriter, HordeCluster cluster)
			{
				bool anyNearby = false;

				foreach (PlayerSnapshot snapshot in snapshotsWriter)
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

			private PlayerHordeGroup DeterminePlayerGroup(LockedListWriter<PlayerSnapshot> snapshotsWriter, HordeCluster cluster)
			{
				PlayerHordeGroup playerGroup = new PlayerHordeGroup();

				foreach (PlayerSnapshot snapshot in snapshotsWriter)
				{
					Vector3 location = snapshot.GetLocation();
					int gamestage = snapshot.GetGamestage();
					bool nearby = IsNearby(location, cluster);

					if (nearby)
						playerGroup.AddPlayer(gamestage);
				}

				return playerGroup;
			}

			private static bool IsNearby(Vector3 location, HordeCluster cluster)
			{
				int distance = 90;
				return Mathf.FloorToInt(Vector3.Distance(location, cluster.GetLocation())) <= distance;
			}
		}
	}
}
