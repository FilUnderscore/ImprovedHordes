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

				if (loaded)
					this.loader.Notify(clustersToChange[true]);

				if (unloaded)
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

			private static bool IsNearby(Vector3 location, HordeCluster cluster)
			{
				int distance = 90;
				return Mathf.FloorToInt(Vector3.Distance(location, cluster.GetLocation())) <= distance;
			}
		}
	}
}
