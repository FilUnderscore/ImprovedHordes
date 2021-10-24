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
        
        private readonly ScoutManager manager;

        public ScoutSpawner(ScoutManager manager) : base(SCOUTS_HORDE_GENERATOR)
        {
            this.manager = manager;
        }

        public override int GetGroupDistance()
        {
            return ScoutManager.CHUNK_RADIUS * 16;
        }

        public override bool GetSpawnPosition(PlayerHordeGroup playerHordeGroup, out Vector3 spawnPosition, out Vector3 targetPosition)
        {
            var radius = this.manager.manager.Random.RandomRange(80, 12 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance));
            Vector2 spawnPosition2D = this.manager.manager.Random.RandomOnUnitCircle * radius;
            spawnPosition = new Vector3(spawnPosition2D.x, 0, spawnPosition2D.y);

            var result = Utils.GetSpawnableY(ref spawnPosition);

            if(!result)
            {
                return GetSpawnPosition(playerHordeGroup, out spawnPosition, out targetPosition);
            }

            targetPosition = CalculateAverageGroupPosition(playerHordeGroup);

            return true;
        }

        protected override void SetAttributes(EntityAlive entity)
        {
            base.SetAttributes(entity);

            entity.IsScoutZombie = true;
        }

        protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;
            float wanderTime = 90f + this.manager.manager.Random.RandomFloat * 4f; // TODO customize?

            // TODO Feral maybe?
            commands.Add(new HordeAICommandDestination(horde.targetPosition, DEST_RADIUS));
            commands.Add(new HordeAICommandWander(wanderTime));

            this.manager.manager.AIManager.Add(entity, horde.horde, true, commands);
        }

        private sealed class ScoutsHordeGenerator : HordeGenerator
        {
            public ScoutsHordeGenerator() : base("scouts")
            {
            }
        }

    }
}
