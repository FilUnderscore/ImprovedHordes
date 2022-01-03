using System;
using System.Collections.Generic;

using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;

using ImprovedHordes.Horde.Wandering.AI.Commands;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Scout
{
    public class ScoutHordeSpawner : HordeSpawner
    {
        private static readonly HordeGenerator SCOUT_HORDE_GENERATOR = new ScoutHordeGenerator();

        private readonly ScoutManager manager;
        
        private readonly Dictionary<PlayerHordeGroup, Vector3> latestTargets = new Dictionary<PlayerHordeGroup, Vector3>();

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
            PlayerHordeGroup playerHordeGroup = this.GetHordeGroupNearPlayer(player);

            if(!latestTargets.ContainsKey(playerHordeGroup))
                latestTargets.Add(playerHordeGroup, Vector3.zero);

            latestTargets[playerHordeGroup] = target;

            this.StartSpawningFor(playerHordeGroup, feral);
        }

        protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;
            float wanderTime = (90f + this.manager.manager.Random.RandomFloat * 4f) * 10f; // Stick around for a long time.

            if (this.latestTargets.ContainsKey(group))
            {
                commands.Add(new HordeAICommandDestination(this.latestTargets[group], DEST_RADIUS));
            }
            else
            {
                Warning($"[Scout] Could not find latest heatmap target for {group}. Using average group position instead.");
                commands.Add(new HordeAICommandDestination(group.CalculateAverageGroupPosition(true), DEST_RADIUS));
            }

            commands.Add(new HordeAICommandWander(wanderTime));
            commands.Add(new HordeAICommandDestination(horde.targetPosition, DEST_RADIUS));

            horde.aiHorde.AddEntity(entity, true, commands);
            AstarManager.Instance.AddLocation(entity.position, 64);
        }

        protected override void PostSpawn(PlayerHordeGroup playerHordeGroup, SpawningHorde horde)
        {
            if(latestTargets.ContainsKey(playerHordeGroup))
                latestTargets.Remove(playerHordeGroup);
        }

        public void Shutdown()
        {
            this.latestTargets.Clear();
        }

        private sealed class ScoutHordeGenerator : HordeGenerator
        {
            public ScoutHordeGenerator() : base("scout")
            {
            }
        }
    }
}
