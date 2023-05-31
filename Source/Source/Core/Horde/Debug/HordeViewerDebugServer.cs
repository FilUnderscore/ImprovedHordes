﻿#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ImprovedHordes.Source.Core.Horde.World.Cluster;
using ImprovedHordes.Source.Core.Threading;
using ImprovedHordes.Source.POI;
using UnityEngine;
using static ImprovedHordes.Source.Core.Horde.World.Cluster.WorldHordeTracker;

namespace ImprovedHordes.Source.Core.Debug
{
    internal struct WorldHordeState
    {
        private int worldSize;
        private List<PlayerSnapshot> players;
        private Dictionary<Type, List<ClusterSnapshot>> clusters;
        private List<WorldPOIScanner.Zone> zones;

        public WorldHordeState(int worldSize, WorldHordeTracker tracker, WorldPOIScanner scanner)
        {
            this.worldSize = worldSize;
            this.players = tracker.GetPlayers();
            this.clusters = tracker.GetClusters();
            this.zones = scanner.GetZones();
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
                EncodeString(writer, player.biome);
            }

            writer.Write(this.clusters.Count);
            foreach(var entry in this.clusters)
            {
                EncodeString(writer, entry.Key.Name);
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

            writer.Write(this.zones.Count);
            foreach(var zone in this.zones)
            {
                writer.Write((int)zone.GetBounds().min.x);
                writer.Write((int)zone.GetBounds().min.z);

                writer.Write((int)zone.GetBounds().size.x);
                writer.Write((int)zone.GetBounds().size.z);
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
        
        public HordeViewerDebugServer(int worldSize, WorldHordeTracker tracker, WorldPOIScanner scanner)
        {
            this.worldSize = worldSize;
            this.tracker = tracker;
            this.scanner = scanner;

            this.listener = new TcpListener(IPAddress.Loopback, PORT);

            this.listener.Start();
            Task.Factory.StartNew(() =>
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
            }, TaskCreationOptions.LongRunning);
        }

        protected override void BeforeTaskRestart()
        {

        }

        protected override void OnTaskFinish(WorldHordeState worldHordeState)
        {
            Parallel.ForEach(clients.ToArray(), client =>
            {
                BinaryWriter writer = new BinaryWriter(client.GetStream());
                worldHordeState.Encode(writer);
            });
        }

        protected override WorldHordeState UpdateAsync(float dt)
        {
            return new WorldHordeState(this.worldSize, this.tracker, this.scanner);
        }
    }
}
#endif