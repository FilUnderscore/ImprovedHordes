﻿using System;
using System.Collections.Generic;
using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;

using ImprovedHordes.Horde.Data;

using ImprovedHordes.Horde.Wandering.AI.Commands;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Wandering
{
    public class WanderingHordeSpawner : HordeSpawner
    {
        private static readonly WanderingHordeGenerator HORDE_GENERATOR = new WanderingHordeGenerator();
        public readonly WanderingHordeManager manager;

        public WanderingHordeSpawner(WanderingHordeManager manager) : base(manager.manager, HORDE_GENERATOR)
        {
            this.manager = manager;
        }

        public override int GetGroupDistance()
        {
            return this.manager.HORDE_PLAYER_GROUP_DISTANCE;
        }

        protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;
            float wanderTime = 90f + this.manager.manager.Random.RandomFloat * 4f;

            if (horde.horde.feral)
            {
                commands.Add(new HordeAICommandDestinationMoving(() => group.CalculateAverageGroupPosition(true), DEST_RADIUS * 2));
                commands.Add(new HordeAICommandWander(wanderTime));
            }
            else
            {
                // Random wander, increase chance of encountering players randomly.
                bool randomWander = this.manager.manager.Random.RandomFloat >= 0.5f;
                
                if (randomWander)
                {
                    float halfwayToEnd = this.manager.manager.Random.RandomRange(0.25f, 0.5f);

                    Vector3 wanderPos = entity.position + (horde.targetPosition - entity.position) * (1 - halfwayToEnd);
                    Utils.GetSpawnableY(ref wanderPos);

                    commands.Add(new HordeAICommandDestination(wanderPos, DEST_RADIUS));
                    commands.Add(new HordeAICommandWander(wanderTime));
                }
            }

            commands.Add(new HordeAICommandDestination(GetRandomNearbyPosition(horde.targetPosition, DEST_RADIUS), DEST_RADIUS));

            AstarManager.Instance.AddLocation(entity.position, 64);
            horde.aiHorde.AddEntity(entity, true, commands);
        }

        protected override void PreSpawn(PlayerHordeGroup group, SpawningHorde horde)
        {
            this.manager.hordes.Add(horde.horde);

            if (horde.horde.group.parent == null && horde.horde.group.children == null)
            {
                this.manager.schedule.AddWeeklyOccurancesForGroup(group.members, horde.horde.group);
            }
            else
            {
                List<HordeGroup> children = horde.horde.group.children;

                if (horde.horde.group.parent != null)
                {
                    HordeGroup parentGroup = horde.horde.group.GetParent();

                    this.manager.schedule.AddWeeklyOccurancesForGroup(group.members, parentGroup);
                    
                    foreach(var child in parentGroup.children)
                    {
                        this.manager.schedule.AddWeeklyOccurancesForGroup(group.members, child);
                    }
                }
                else
                {
                    this.manager.schedule.AddWeeklyOccurancesForGroup(group.members, horde.horde.group);
                }

                if (children != null)
                {
                    foreach (var child in children)
                    {
                        this.manager.schedule.AddWeeklyOccurancesForGroup(group.members, child);
                    }
                }
            }

            Log("[Wandering Horde] Horde for group: {0}", group.ToString());
            Log("[Wandering Horde] Horde Group: {0}", horde.horde.group.name);
            Log("[Wandering Horde] GS: {0}", group.GetGroupGamestage());
            Log("[Wandering Horde] Start Pos #1 (offset from farthest player): " + horde.spawnPositions.Peek().ToString());
            Log("[Wandering Horde] End Pos: " + horde.targetPosition.ToString());
            Log("[Wandering Horde] Horde size: " + horde.horde.count);
        }

        public void SpawnWanderingHordes()
        {
            Log("[Wandering Horde] Occurance {0} Spawning", this.manager.schedule.currentOccurance + 1);

            this.manager.state = WanderingHordeManager.EHordeState.StillAlive;
            this.StartSpawningFor(GetAllHordeGroups(), this.manager.schedule.GetCurrentOccurance().feral);
        }

        private sealed class WanderingHordeGenerator : HordeGenerator
        {
            public WanderingHordeGenerator() : base("wandering")
            { }

            public override bool CanHordeGroupBePicked(PlayerHordeGroup playerGroup, HordeGroup group, string biomeAtPosition)
            {
                WanderingHordeManager wanderingHorde = ImprovedHordesManager.Instance.WanderingHorde;

                if (group.MaxWeeklyOccurances != null)
                {
                    var maxWeeklyOccurances = group.MaxWeeklyOccurances.Evaluate();

                    var weeklyOccurancesForPlayer = wanderingHorde.schedule.GetAverageWeeklyOccurancesForGroup(playerGroup, group);

                    if (weeklyOccurancesForPlayer >= maxWeeklyOccurances)
                        return false;

                    if (maxWeeklyOccurances > 0)
                    {
                        float diminishedChance = (float)Math.Pow(1 / maxWeeklyOccurances, weeklyOccurancesForPlayer);

                        if (wanderingHorde.manager.Random.RandomFloat > diminishedChance)
                            return false;
                    }
                }

                return base.CanHordeGroupBePicked(playerGroup, group, biomeAtPosition);
            }
        }
    }
}