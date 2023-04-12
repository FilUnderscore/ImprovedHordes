using HarmonyLib;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster
{
    public sealed class WorldHordeClusterTracker
    {
        private struct PlayerSnapshot
        {
            public Vector3 location;
            public int gamestage;

            public PlayerSnapshot(Vector3 location, int gamestage)
            {
                this.location = location;
                this.gamestage = gamestage;
            }
        }

        private Task UpdateTask;

        private List<HordeCluster> toAdd = new List<HordeCluster>();
        private ConcurrentQueue<HordeCluster> toRemove = new ConcurrentQueue<HordeCluster>();

        private int clusterCount = 0;
        private List<HordeCluster> clusters = new List<HordeCluster>();

        private List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();
        private List<WorldEvent> events = new List<WorldEvent>();

        public void Update(float dt)
        {
            if(UpdateTask != null && UpdateTask.IsCompleted)
            {
                snapshots.Clear();

                foreach(var cluster in toAdd)
                {
                    clusters.Add(cluster);
                    //Log.Out("Added cluster");
                }

                toAdd.Clear();

                while(toRemove.TryDequeue(out HordeCluster cluster))
                {
                    clusters.Remove(cluster);
                    Log.Out("Removed cluster");
                }

                this.clusterCount = this.clusters.Count;
            }

            if(UpdateTask == null || UpdateTask.IsCompleted)
            {
                foreach(var player in GameManager.Instance.World.Players.list)
                {
                    snapshots.Add(new PlayerSnapshot(player.position, player.gameStage));
                }

                this.UpdateTask = Task.Run(async () =>
                {
                    await UpdateAsync(snapshots, dt);
                });
            }
        }

        public int GetClusterCount()
        {
            return this.clusterCount;
        }

        private async Task UpdateAsync(List<PlayerSnapshot> players, float dt)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(this.clusters, cluster =>
                {
                    IEnumerable<PlayerSnapshot> nearby = players.Where(player =>
                    {
                        return Vector3.Distance(player.location, cluster.GetLocation()) <= 90;
                    });

                    if(nearby.Any() && !cluster.IsSpawned())
                    {
                        PlayerHordeGroup group = new PlayerHordeGroup();
                        nearby.Do(player => group.AddPlayer(player.gamestage));

                        cluster.Spawn(group);
                    }
                    else if(!nearby.Any() && cluster.IsSpawned())
                    {
                        cluster.Despawn();
                    }

                    if(cluster.IsSpawned())
                    {
                        cluster.UpdatePosition();
                    }

                    if(cluster.IsDead())
                    {
                        toRemove.Enqueue(cluster);
                    }
                });
            });
        }

        public void Add(HordeCluster cluster)
        {
            toAdd.Add(cluster);
        }
    }
}