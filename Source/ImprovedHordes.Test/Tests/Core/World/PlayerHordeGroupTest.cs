using ImprovedHordes.Core.World.Horde;
using UnityEngine;

namespace ImprovedHordes.Test.Tests.Core.World
{
    public sealed class PlayerHordeGroupTest
    {
        private WorldPlayerTracker worldPlayerTracker;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            this.worldPlayerTracker = new WorldPlayerTracker();
        }

        private PlayerSnapshot CreatePlayer(Vector3 position)
        {
            return new PlayerSnapshot(null, null, position);
        }

        private PlayerSnapshot CreatePlayer(float x, float y, float z)
        {
            return CreatePlayer(new Vector3(x, y, z));
        }

        private void GroupAssertThat(int maxViewDistance, int expectedGroupCount, params PlayerSnapshot[] players)
        {
            List<PlayerHordeGroup> groups = this.worldPlayerTracker.GroupPlayers(maxViewDistance, players.ToList());
            Assert.That(groups.Count, Is.EqualTo(expectedGroupCount), "Incorrect number of player groups formed.");
        }

        [Test]
        public void Group1Test()
        {
            GroupAssertThat(120, 1, CreatePlayer(0, 0, 0));
        }

        [Test]
        public void Group2Test() 
        {
            GroupAssertThat(120, 1, CreatePlayer(0, 0, 0), CreatePlayer(120, 0, 0));
        }

        [Test]
        public void Group3Test()
        {
            GroupAssertThat(120, 1, CreatePlayer(0, 0, 0), CreatePlayer(120, 0, 0), CreatePlayer(240, 0, 0));
        }

        [Test]
        public void Group4_0Test()
        {
            GroupAssertThat(120, 1, CreatePlayer(0, 0, 0), CreatePlayer(120, 0, 0), CreatePlayer(240, 0, 0), CreatePlayer(180, 0, 60));
        }

        [Test]
        public void Group4_1Test()
        {
            GroupAssertThat(120, 2, CreatePlayer(0, 0, 0), CreatePlayer(120, 0, 0), CreatePlayer(240, 0, 0), CreatePlayer(180, 0, 180));
        }

        [Test]
        public void Group4_2Test()
        {
            GroupAssertThat(120, 2, CreatePlayer(0, 0, 0), CreatePlayer(120, 0, 0), CreatePlayer(240, 0, 0), CreatePlayer(180, 0, 180), CreatePlayer(180, 0, 300));
        }
    }
}
