#if DEBUG
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Threading;
using ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet.Game;
using ImprovedHordes.Core.World.Horde.Debug.ViewerServer.Packet.Login;
using ImprovedHordes.POI;

namespace ImprovedHordes.Core.World.Horde.Debug
{
    internal sealed class HordeViewerDebugServer : Threaded
    {
        private readonly int PORT = 9000;

        private readonly int worldSize;
        private readonly WorldPOIScanner scanner;

        private readonly TcpListener listener;

        private readonly List<TcpClient> clients = new List<TcpClient>();
        private bool running = false;

        private byte[] biomesTexData;
        private int biomesTexHeight, biomesTexWidth;

        private readonly ThreadSubscriber<List<PlayerHordeGroup>> playerGroups;
        private readonly ThreadSubscriber<Dictionary<Type, List<ClusterSnapshot>>> clusters;

        public HordeViewerDebugServer(ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory, int worldSize, WorldHordeTracker tracker, WorldPOIScanner scanner) : base(loggerFactory, randomFactory)
        {
            this.worldSize = worldSize;
            this.scanner = scanner;

            this.playerGroups = tracker.GetPlayerTracker().Subscribe();
            this.clusters = tracker.GetClustersSubscription().Subscribe();

            // load biomes image
            this.LoadBiomesImage();
            
            this.listener = new TcpListener(IPAddress.Loopback, PORT);
        }

        private void LoadBiomesImage()
        {
            Task.Run(() =>
            {
                this.Logger.Info("Loading biomes.png.");

                string biomesImagePath = PathAbstractions.WorldsSearchPaths.GetLocation(GamePrefs.GetString(EnumGamePrefs.GameWorld)).FullPath + "/biomes";
                Bitmap biomesTex = new Bitmap(biomesImagePath + ".png");

                byte[] biomesTexData = new byte[biomesTex.Width * biomesTex.Height * 3];

                for (int y = 0; y < biomesTex.Height; y++)
                {
                    for (int x = 0; x < biomesTex.Width * 3; x += 3)
                    {
                        Color pixel = biomesTex.GetPixel(x / 3, y);

                        biomesTexData[y * (biomesTex.Width * 3) + x] = pixel.R;
                        biomesTexData[y * (biomesTex.Width * 3) + x + 1] = pixel.G;
                        biomesTexData[y * (biomesTex.Width * 3) + x + 2] = pixel.B;
                    }
                }

                this.Logger.Info("Loaded biomes.png. Uncompressed length: " + biomesTexData.Length);

                // compress biomes image
                using (MemoryStream imageStream = new MemoryStream())
                {
                    using (GZipStream compressionStream = new GZipStream(imageStream, CompressionLevel.Optimal))
                    {
                        compressionStream.Write(biomesTexData, 0, biomesTexData.Length);
                    }

                    this.biomesTexData = imageStream.ToArray();
                }

                this.biomesTexWidth = biomesTex.Width;
                this.biomesTexHeight = biomesTex.Height;

                this.Logger.Info("Compressed biomes.png. Compressed length: " + this.biomesTexData.Length);
            }).ContinueWith(t =>
            {
                if(t.Exception != null)
                {
                    this.Logger.Error("Task failed: " + t.Exception.Message);
                    this.Logger.Error("ST: " + t.Exception.StackTrace);

                    if(t.Exception.InnerException != null)
                    {
                        this.Logger.Exception(t.Exception.InnerException);
                    }
                }
            });
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
                        TcpClient client = this.listener.AcceptTcpClient();
                        this.Logger.Info($"New client {(client.Client.RemoteEndPoint as IPEndPoint).Address} connected.");

                        SendLoginPackets(client);
                        this.clients.Add(client);
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

        private void SendLoginPackets(TcpClient client)
        {
            BinaryWriter writer = new BinaryWriter(client.GetStream());

            new InitPacket(worldSize, GameStats.GetInt(EnumGameStats.AllowedViewDistance)).Send(writer);
            new BiomesPacket(this.biomesTexWidth, this.biomesTexHeight, this.biomesTexData).Send(writer);
            new ZonesPacket(this.scanner.GetAllZones()).Send(writer);
        }

        protected override void UpdateAsync(float dt)
        {
            PlayersPacket playersPacket = null;
            ClustersPacket clustersPacket = null;

            if(this.playerGroups.TryGet(out var playerGroups))
            {
                playersPacket = new PlayersPacket(playerGroups);
            }

            if(this.clusters.TryGet(out var clusters))
            {
                clustersPacket = new ClustersPacket(clusters);
            }

            for(int i = 0; i < this.clients.Count; i++)
            {
                var client = this.clients[i];

                try
                {
                    BinaryWriter writer = new BinaryWriter(client.GetStream());

                    if (playersPacket != null)
                    {
                        playersPacket.Send(writer);
                    }

                    if (clustersPacket != null)
                    {
                        clustersPacket.Send(writer);
                    }
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