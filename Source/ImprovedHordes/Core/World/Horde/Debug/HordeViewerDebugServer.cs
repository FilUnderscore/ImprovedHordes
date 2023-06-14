﻿#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ImprovedHordes.Core.Abstractions.Logging;
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

                    writer.Write(player.gamestage);
                    EncodeString(writer, player.biome);
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

                writer.Write(zone.GetDensity());
                writer.Write(zone.GetCount());
                writer.Write(zone.GetAverageDistanceBetweenZones());
                writer.Write(zone.GetAverageWeight());
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

    internal sealed class HordeViewerDebugServer : MainThreadSynchronizedTask<WorldHordeState>
    {
        private readonly int PORT = 9000;

        private readonly int worldSize;
        private readonly WorldHordeTracker tracker;
        private readonly WorldPOIScanner scanner;

        private readonly TcpListener listener;

        private readonly List<TcpClient> clients = new List<TcpClient>();
        private bool running = true;

        private readonly ThreadSubscriber<List<PlayerSnapshot>> players;
        private readonly ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> clusters;

        public HordeViewerDebugServer(ILoggerFactory loggerFactory, int worldSize, WorldHordeTracker tracker, WorldPOIScanner scanner) : base(loggerFactory)
        {
            this.worldSize = worldSize;
            this.tracker = tracker;
            this.scanner = scanner;

            this.players = tracker.GetPlayersSubscription().Subscribe();
            this.clusters = tracker.GetClustersSubscription().Subscribe();

            this.listener = new TcpListener(IPAddress.Loopback, PORT);

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
                    catch(SocketException ex)
                    {
                        this.Logger.Error($"Socket exception occurred while listening for new clients. {ex.Message}");
                    }
                }
                this.Logger.Info("Shutdown debug server.");
            }, TaskCreationOptions.LongRunning);
        }

        protected override void BeforeTaskRestart()
        {

        }

        protected override void OnTaskFinish(WorldHordeState worldHordeState)
        {
            Parallel.ForEach(clients.ToArray(), client =>
            {
                try
                {
                    BinaryWriter writer = new BinaryWriter(client.GetStream());
                    worldHordeState.Encode(writer);
                }
                catch(Exception)
                {
                    this.Logger.Warn("Client disconnected.");
                    this.clients.Remove(client);
                }
            });
        }

        protected override WorldHordeState UpdateAsync(float dt)
        {
            return new WorldHordeState(this.worldSize, this.scanner, this.players, this.clusters);
        }

        protected override void Shutdown()
        {
            base.Shutdown();

            this.running = false;
            this.listener.Stop();
        }
    }
}
#endif