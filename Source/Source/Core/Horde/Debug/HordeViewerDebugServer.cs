#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Threading;
using UnityEngine;
using static ImprovedHordes.Source.Core.Horde.World.Cluster.WorldHordeTracker;

namespace ImprovedHordes.Source.Core.Debug
{
    internal struct WorldHordeState
    {
        private int worldSize;
        private List<PlayerSnapshot> players;
        private Dictionary<Type, List<ClusterSnapshot>> clusters;

        public WorldHordeState(int worldSize, WorldHordeTracker tracker)
        {
            this.worldSize = worldSize;
            this.players = tracker.GetPlayers();
            this.clusters = tracker.GetClusters();
        }

        public void Encode(BinaryWriter writer)
        {
            writer.Write(this.worldSize);

            writer.Write(this.players.Count);
            foreach(var player in this.players)
            {
                Vector3 location = player.location;

                writer.Write(location.x);
                writer.Write(location.y);
                writer.Write(location.z);

                writer.Write(player.gamestage);
            }

            writer.Write(this.clusters.Count);
            foreach(var entry in this.clusters)
            {
                writer.Write(entry.Value.Count);

                foreach(var cluster in entry.Value)
                {
                    Vector3 location = cluster.location;

                    writer.Write(location.x);
                    writer.Write(location.y);
                    writer.Write(location.z);

                    writer.Write(cluster.density);
                }
            }
        }
    }

    internal sealed class HordeViewerDebugServer : MainThreadSynchronizedTask<WorldHordeState>
    {
        private readonly int PORT = 9000;

        private readonly int worldSize;
        private readonly WorldHordeTracker tracker;

        private readonly TcpListener listener;

        private readonly List<TcpClient> clients = new List<TcpClient>();
        
        public HordeViewerDebugServer(int worldSize, WorldHordeTracker tracker)
        {
            this.worldSize = worldSize;
            this.tracker = tracker;

            this.listener = new TcpListener(IPAddress.Loopback, PORT);

            this.listener.Start();
            Task.Run(() =>
            {
                Log.Out($"Started debug server listening on port {PORT}.");

                while (ImprovedHordesCore.TryGetInstance(out var instance) && instance.IsInitialized())
                {
                    try
                    {
                        this.clients.Add(this.listener.AcceptTcpClient());
                        Log.Out("New client connected.");
                    }
                    catch(SocketException ex)
                    {
                        Log.Error($"Socket exception occurred while listening for new clients. {ex.Message}");
                    }
                }

                Log.Out("Shutdown debug server.");
            });
        }

        public override void BeforeTaskRestart()
        {

        }

        public override void OnTaskFinish(WorldHordeState worldHordeState)
        {
            Task.Run(() =>
            {
                Parallel.ForEach(clients.ToArray(), client =>
                {
                    BinaryWriter writer = new BinaryWriter(client.GetStream());
                    worldHordeState.Encode(writer);
                });
            });
        }

        public override async Task<WorldHordeState> UpdateAsync(float dt)
        {
            return await UpdateStateAsync();
        }

        public async Task<WorldHordeState> UpdateStateAsync()
        {
            return await Task.Run(() =>
            {
                return new WorldHordeState(this.worldSize, this.tracker);
            });
        }
    }
}
#endif