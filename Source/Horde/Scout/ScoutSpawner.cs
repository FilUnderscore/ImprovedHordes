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
        private readonly ScoutManager manager;

        public ScoutSpawner(ScoutManager manager) : base(manager.manager, SCOUTS_HORDE_GENERATOR)
        {
            this.manager = manager;
        }

        public override int GetGroupDistance()
        {
            return ScoutManager.CHUNK_RADIUS * 16;
        }

        protected override void SetAttributes(EntityAlive entity)
        {
            base.SetAttributes(entity);

            entity.IsScoutZombie = true;
        }

        public void StartSpawningFor(EntityPlayer nearestPlayer, bool feral, Vector3 target)
        {
            latestTarget = target;

            this.StartSpawningFor(nearestPlayer, feral);
        }

        protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;
            float wanderTime = 90f + this.manager.manager.Random.RandomFloat * 4f; // TODO customize?

            commands.Add(new HordeAICommandDestination(latestTarget, DEST_RADIUS));
            commands.Add(new HordeAICommandWander(wanderTime));
            commands.Add(new HordeAICommandDestination(horde.targetPosition, DEST_RADIUS));

            this.manager.manager.AIManager.Add(entity, horde.horde, true, commands);
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
