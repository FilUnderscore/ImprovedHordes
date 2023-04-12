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
        private List<HordeCluster> clusters = new List<HordeCluster>();

        public void Update()
        {
            if(UpdateTask != null && UpdateTask.IsCompleted)
            {
                foreach(var cluster in toAdd)
                {
                    clusters.Add(cluster);
                    Log.Out("Added cluster");
                }

                toAdd.Clear();
            }

            if(UpdateTask == null || UpdateTask.IsCompleted)
            {
                List<PlayerSnapshot> snapshots = new List<PlayerSnapshot>();
                foreach(var player in GameManager.Instance.World.Players.list)
                {
                    snapshots.Add(new PlayerSnapshot(player.position, player.gameStage));
                }

                this.UpdateTask = Task.Run(async () =>
                {
                    await UpdateAsync(snapshots.AsReadOnly());
                });
            }
        }

        private async Task UpdateAsync(IReadOnlyCollection<PlayerSnapshot> players)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(this.clusters, cluster =>
                {
                    List<PlayerSnapshot> nearby = players.AsParallel().Where(player =>
                    {
                        return Vector3.Distance(player.location, cluster.GetLocation()) <= 90;
                    }).ToList();

                    if(nearby.Any() && !cluster.IsSpawned())
                    {
                        PlayerHordeGroup group = new PlayerHordeGroup();
                        nearby.ForEach(player => group.AddPlayer(player.gamestage));

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
                });
            });
        }

        public void Add(HordeCluster cluster)
        {
            toAdd.Add(cluster);
        }
    }
}