using System;
using System.Collections.Generic;

using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;

namespace ImprovedHordes.Horde.Scout
{
    public class ScoutHordeSpawner : HordeSpawner
    {
        private static readonly HordeGenerator SCOUT_HORDE_GENERATOR = new ScoutHordeGenerator();

        private readonly ScoutManager manager;

        public ScoutHordeSpawner(ScoutManager manager) : base(manager.manager, SCOUT_HORDE_GENERATOR)
        {
            this.manager = manager;
        }

        public override int GetGroupDistance()
        {
            return ScoutManager.CHUNK_RADIUS * 16; 
        }

        protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;

            commands.Add(new HordeAICommandDestination(horde.targetPosition, DEST_RADIUS));

            this.manager.manager.AIManager.Add(entity, horde.horde, false, commands);
        }

        private sealed class ScoutHordeGenerator : HordeGenerator
        {
            public ScoutHordeGenerator() : base("scout")
            {
            }
        }
    }
}
