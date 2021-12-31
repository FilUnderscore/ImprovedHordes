﻿using System;
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
        private Vector3 latestTarget = Vector3.zero;

        public ScoutHordeSpawner(ScoutManager manager) : base(manager.manager, SCOUT_HORDE_GENERATOR)
        {
            this.manager = manager;
        }

        public override int GetGroupDistance()
        {
            return this.manager.CHUNK_RADIUS * 16; 
        }

        public void StartSpawningFor(EntityPlayer player, bool feral, Vector3 target)
        {
            this.latestTarget = target; // TODO better way to do this?
            
            this.StartSpawningFor(this.GetHordeGroupNearPlayer(player), feral);
        }

        protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;

            commands.Add(new HordeAICommandDestination(this.latestTarget, DEST_RADIUS));

            horde.aiHorde.AddEntity(entity, false, commands);
            AstarManager.Instance.AddLocation(entity.position, 64);
        }

        private sealed class ScoutHordeGenerator : HordeGenerator
        {
            public ScoutHordeGenerator() : base("scout")
            {
            }
        }
    }
}