using System.Collections.Generic;
using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;

namespace ImprovedHordes.Horde.Heat
{
    public class PatrolHordeSpawner : HordeSpawner
    {
        private static readonly HordeGenerator PATROL_HORDE_GENERATOR = new PatrolHordeGenerator();

        public PatrolHordeSpawner(ImprovedHordesManager manager) : base(manager, PATROL_HORDE_GENERATOR)
        {

        }

        public override int GetGroupDistance()
        {
            return HordeAreaHeatTracker.RADIUS_SQUARED * 16 / 2;
        }

        protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;
            float wanderTime = 90f + this.manager.Random.RandomFloat * 4f;

            commands.Add(new HordeAICommandDestination(GetRandomNearbyPosition(horde.targetPosition, DEST_RADIUS), DEST_RADIUS));

            AstarManager.Instance.AddLocation(entity.position, 64);
            horde.aiHorde.AddEntity(entity, true, commands);
        }

        class PatrolHordeGenerator : HordeGenerator
        {
            public PatrolHordeGenerator() : base("patrol")
            {
            }
        }
    }
}