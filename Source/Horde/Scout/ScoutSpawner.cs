using System;
using System.Collections.Generic;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;
using ImprovedHordes.Horde.Wandering.AI.Commands;

using UnityEngine;

namespace ImprovedHordes.Horde.Scout
{
    public class ScoutSpawner : HordeSpawner
    {
        private static readonly HordeGenerator SCOUTS_HORDE_GENERATOR = new ScoutsHordeGenerator(); // Scouts - e.g. Screamer

        private Vector3 latestTarget = Vector3.zero;
        private new readonly ScoutManager manager;

        public ScoutSpawner(ScoutManager manager) : base(manager.manager, SCOUTS_HORDE_GENERATOR)
        {
            this.manager = manager;
        }

        public override int GetGroupDistance()
        {
            return this.manager.CHUNK_RADIUS * 16;
        }

        protected override void SetAttributes(EntityAlive entity)
        {
            base.SetAttributes(entity);

            entity.IsScoutZombie = true;
        }

        public void StartSpawningFor(PlayerHordeGroup group, bool feral, Vector3 target)
        {
            latestTarget = target;

            this.StartSpawningFor(group, feral);
        }

        protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;
            float wanderTime = 450f + (this.manager.manager.Random.RandomFloat * 125f); // TODO customize?

            commands.Add(new HordeAICommandDestination(latestTarget, DEST_RADIUS));
            commands.Add(new HordeAICommandWander(wanderTime));
            commands.Add(new HordeAICommandDestination(horde.targetPosition, DEST_RADIUS));

            horde.aiHorde.AddEntity(entity, true, commands);
            AstarManager.Instance.AddLocation(entity.position, 64);
        }

        private sealed class ScoutsHordeGenerator : HordeGenerator
        {
            public ScoutsHordeGenerator() : base("scouts")
            {
            }
        }

    }
}
