using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.Abstractions.World.Random;
using ImprovedHordes.Core.Threading.Request;
using ImprovedHordes.Core.World.Horde.Cluster;
using System;

namespace ImprovedHordes.Core.World.Horde.Spawn.Request
{
    public sealed class HordeClusterSpawnRequest : IMainThreadRequest
    {
        private readonly ILogger logger;

        private readonly HordeEntityGenerator generator;

        private readonly WorldHorde horde;

        private readonly HordeCluster cluster;

        private readonly int size;
        private int index;

        private readonly Action onSpawnedAction;

        private readonly IRandomFactory<IWorldRandom> randomFactory;
        private readonly IWorldRandom random;

        public HordeClusterSpawnRequest(ILoggerFactory loggerFactory, IRandomFactory<IWorldRandom> randomFactory, WorldHorde horde, HordeCluster cluster, PlayerHordeGroup playerGroup, Action onSpawned)
        {
            this.logger = loggerFactory.Create(typeof(HordeClusterSpawnRequest));
            
            this.horde = horde;

            this.cluster = cluster;

            this.randomFactory = randomFactory;
            this.random = randomFactory.CreateRandom(this.cluster.GetHashCode());
            this.generator = DetermineEntityGenerator(playerGroup, this.random);

            this.size = this.generator.DetermineEntityCount(cluster.GetDensity());
            this.index = 0;

            this.onSpawnedAction = onSpawned;
        }

        private HordeEntityGenerator DetermineEntityGenerator(PlayerHordeGroup playerGroup, IRandom random)
        {
            // TODO: this.cluster.GetPreviousHordeEntityGenerator();
            return this.cluster.GetHorde().CreateEntityGenerator(playerGroup, random);
        }

        public bool IsDone()
        {
            return this.index >= this.size;
        }

        public void OnCleanup()
        {
            this.randomFactory.FreeRandom(this.random);
            this.onSpawnedAction?.Invoke();
        }

        private const float SPAWN_DELAY = 2.0f;
        private float spawnTicks;

        public void TickExecute(float dt)
        {
            if (!this.cluster.Spawning) // If the cluster state changes from spawning, then cancel further spawns.
            {
                this.Cancel();
                return;
            }

            if (--spawnTicks > 0.0f) // Slight spawn delay prevents immediate stuttering when spawning large groups of enemies.
                return;
            else
                spawnTicks = SPAWN_DELAY;

            HordeClusterEntity entity = new HordeClusterEntity(cluster, this.generator.GetEntityClassId(this.random), this.horde.GetLocation(), this.horde.GetCharacteristics());
            this.cluster.AddEntity(entity);
            
            this.index++;
        }

        public int GetCount()
        {
            return this.size;
        }

        public int GetRemaining()
        {
            return this.size - this.index;
        }

        public void Cancel()
        {
            this.index = this.size;
        }
    }
}
