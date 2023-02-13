using System;
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
        public new readonly WanderingHordeManager manager;

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
            float wanderTime = HordeAIManager.WANDER_TIME + this.manager.manager.Random.RandomFloat * 4f;

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
                this.manager.schedule.AddWeeklyOccurrencesForGroup(group.members, horde.horde.group);
            }
            else
            {
                List<HordeGroup> children = horde.horde.group.children;

                if (horde.horde.group.parent != null)
                {
                    HordeGroup parentGroup = horde.horde.group.GetParent();

                    this.manager.schedule.AddWeeklyOccurrencesForGroup(group.members, parentGroup);
                    
                    foreach(var child in parentGroup.children)
                    {
                        this.manager.schedule.AddWeeklyOccurrencesForGroup(group.members, child);
                    }
                }
                else
                {
                    this.manager.schedule.AddWeeklyOccurrencesForGroup(group.members, horde.horde.group);
                }

                if (children != null)
                {
                    foreach (var child in children)
                    {
                        this.manager.schedule.AddWeeklyOccurrencesForGroup(group.members, child);
                    }
                }
            }

            Log("[Wandering Horde] Horde for group: {0}", group.ToString());
            Log("[Wandering Horde] Horde Group: {0}", horde.horde.group.name);
            Log("[Wandering Horde] GS: {0}", horde.horde.gamestage);
            Log("[Wandering Horde] Start Pos #1 (offset from farthest player): " + horde.spawnPositions.Peek().ToString());
            Log("[Wandering Horde] End Pos: " + horde.targetPosition.ToString());
            Log("[Wandering Horde] Horde size: " + horde.horde.count);
        }

        public bool SpawnWanderingHordes()
        {
            var playerGroups = GetAllHordeGroups();

            if (playerGroups.Count == 0)
                return false;

            Log("[Wandering Horde] Occurrence {0} Spawning", this.manager.schedule.currentOccurrence + 1);

            this.manager.state = WanderingHordeManager.EHordeState.StillAlive;
            this.StartSpawningFor(playerGroups, this.manager.schedule.GetCurrentOccurrence().feral);

            return true;
        }

        private sealed class WanderingHordeGenerator : HordeGenerator
        {
            public WanderingHordeGenerator() : base("wandering")
            { }

            protected override bool CanHordeGroupBePicked(HordeGenerationData hordeGenerationData, HordeGroup group)
            {
                WanderingHordeManager wanderingHorde = ImprovedHordesManager.Instance.WanderingHorde;

                if (group.MaxWeeklyOccurrences != null)
                {
                    var maxWeeklyOccurrences = group.MaxWeeklyOccurrences.Evaluate();

                    var weeklyOccurrencesForPlayer = wanderingHorde.schedule.GetAverageWeeklyOccurrencesForGroup(hordeGenerationData.PlayerGroup, group);

                    if (weeklyOccurrencesForPlayer >= maxWeeklyOccurrences)
                        return false;

                    if (maxWeeklyOccurrences > 0)
                    {
                        float diminishedChance = (float)Math.Pow(1 / maxWeeklyOccurrences, weeklyOccurrencesForPlayer);

                        if (wanderingHorde.manager.Random.RandomFloat > diminishedChance)
                            return false;
                    }
                }

                return base.CanHordeGroupBePicked(hordeGenerationData, group);
            }
        }
    }
}