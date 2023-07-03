#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Threading;
using ImprovedHordes.POI;
using UnityEngine;
using static ImprovedHordes.Core.World.Horde.WorldHordeTracker;

namespace ImprovedHordes.Core.World.Horde.Debug
{
    internal readonly struct WorldHordeState
    {
        private readonly int worldSize;
        private readonly ThreadSubscriber<List<PlayerSnapshot>> players;
        private readonly ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> clusters;
        private readonly List<WorldPOIScanner.POIZone> zones;

        public WorldHordeState(int worldSize, WorldPOIScanner scanner, ThreadSubscriber<List<PlayerSnapshot>> players, ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> clusters)
        {
            this.worldSize = worldSize;
            this.players = players;
            this.clusters = clusters;
            this.zones = scanner.GetZones();
        }

        public void Encode(BinaryWriter writer)
        {
            writer.Write(this.worldSize);

            if (this.players.TryGet(out var players))
            {
                writer.Write(players.Count);
                foreach (var player in players)
                {
                    Vector3 location = player.location;

                    writer.Write(location.x);
                    writer.Write(location.y);
                    writer.Write(location.z);

                    writer.Write(player.player.gameStage);
                    EncodeString(writer, player.player.biomeStandingOn?.m_sBiomeName);
                }
            }
            else
            {
                writer.Write(0);
            }

            if (this.clusters.TryGet(out var clusters))
            {
                writer.Write(clusters.Count);
                foreach (var entry in clusters)
                {
                    EncodeString(writer, entry.Key.Name);
                    writer.Write(entry.Value.Count);

                    foreach (var cluster in entry.Value)
                    {
                        Vector3 location = cluster.location;

                        writer.Write(location.x);
                        writer.Write(location.y);
                        writer.Write(location.z);

                        writer.Write(cluster.density);
                    }
                }
            }
            else
            {
                writer.Write(0);
            }

            writer.Write(this.zones.Count);
            foreach(var zone in this.zones)
            {
                writer.Write((int)zone.GetBounds().min.x);
                writer.Write((int)zone.GetBounds().min.z);

                writer.Write((int)zone.GetBounds().size.x);
                writer.Write((int)zone.GetBounds().size.z);

                writer.Write(0.0f);
                writer.Write(0);
                writer.Write(0.0f);
                writer.Write(0.0f);
            }
        }

        private void EncodeString(BinaryWriter writer, string str)
        {
            bool valid = str != null && str.Length > 0;

            writer.Write(valid);

            if (valid)
            {
                writer.Write(str.Length);
                writer.Write(Encoding.UTF8.GetBytes(str));
            }
        }
    }

    internal sealed class HordeViewerDebugServer : Threaded
    {
        private readonly int PORT = 9000;

        private readonly int worldSize;
        private readonly WorldHordeTracker tracker;
        private readonly WorldPOIScanner scanner;

        private readonly TcpListener listener;

        private readonly List<TcpClient> clients = new List<TcpClient>();
        private bool running = false;

        private readonly ThreadSubscriber<List<PlayerSnapshot>> players;
        private readonly ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> clusters;

        public HordeViewerDebugServer(ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory, int worldSize, WorldHordeTracker tracker, WorldPOIScanner scanner) : base(loggerFactory, randomFactory)
        {
            this.worldSize = worldSize;
            this.tracker = tracker;
            this.scanner = scanner;

            this.players = tracker.GetPlayerTracker().Subscribe();
            this.clusters = tracker.GetClustersSubscription().Subscribe();

            this.listener = new TcpListener(IPAddress.Loopback, PORT);
        }

        public void StartServer()
        {
            this.Start();
            this.running = true;

            this.listener.Start();
            Task.Factory.StartNew(() =>
            {
                this.Logger.Info($"Started debug server listening on port {PORT}.");

                while (this.running)
                {
                    try
                    {
                        this.clients.Add(this.listener.AcceptTcpClient());
                        this.Logger.Info("New client connected.");
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.Interrupted)
                            break;

                        this.Logger.Error($"Socket exception occurred while listening for new clients. {ex.Message}");
                    }
                }
                this.Logger.Info("Shutdown debug server.");
            }, TaskCreationOptions.LongRunning);
        }

        public bool Started
        {
            get
            {
                return this.running;
            }
        }

        protected override void UpdateAsync(float dt)
        {
            WorldHordeState state = new WorldHordeState(this.worldSize, this.scanner, this.players, this.clusters);

            foreach(var client in this.clients)
            {
                try
                {
                    BinaryWriter writer = new BinaryWriter(client.GetStream());
                    state.Encode(writer);
                }
                catch (Exception)
                {
                    this.Logger.Warn("Client disconnected.");
                    this.clients.Remove(client);
                }
            }
        }

        protected override void OnShutdown()
        {
            foreach(var client in this.clients) 
            {
                client.Close();
            }

            this.running = false;
            this.listener.Stop();
        }
    }
}
#endif