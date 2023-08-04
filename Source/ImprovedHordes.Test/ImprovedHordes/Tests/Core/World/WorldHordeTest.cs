using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde;
using ImprovedHordes.Core.World.Horde.Cluster;
using ImprovedHordes.Core.World.Horde.Spawn;
using UnityEngine;

namespace ImprovedHordes.Test
{
    public sealed class WorldHordeTest
    {
        private ILoggerFactory loggerFactory;
        private IRandomFactory<IWorldRandom> randomFactory;
        private MainThreadRequestProcessor mainThreadRequestProcessor;

        private WorldHorde horde;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            this.loggerFactory = new TestLoggerFactory();
            this.randomFactory = new TestRandomFactory();

            this.mainThreadRequestProcessor = new MainThreadRequestProcessor();
        }

        [SetUp]
        public void Setup()
        {
            HordeCluster cluster = new HordeCluster(new TestHorde(), 1.0f, null);
            HordeSpawnParams spawnParams = new HordeSpawnParams(15);
            HordeSpawnData spawnData = new HordeSpawnData(spawnParams, null);

            this.horde = new WorldHorde(Vector3.zero, spawnData, cluster, this.randomFactory, null);
        }

        [Test]
        public void Split1Test() 
        {
            Assert.True(this.horde.Split(0.1f, this.loggerFactory, this.mainThreadRequestProcessor, out List<WorldHorde> newHordes), "Failed to split hordes.");
            Assert.That(newHordes.Count, Is.EqualTo(10 - 1), "Split hordes are not accurate.");
        }

        [Test]
        public void Split2Test()
        {
            this.horde.GetClusters().Add(new HordeCluster(new TestHorde(), 10.0f, null));

            Assert.True(this.horde.Split(0.1f, this.loggerFactory, this.mainThreadRequestProcessor, out List<WorldHorde> newHordes), "Failed to split hordes.");
            Assert.That(newHordes.Count, Is.EqualTo(110 - 2), "Split hordes are not accurate.");
        }
    }
}
