using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;

using ImprovedHordes.Horde.Wandering.AI.Commands;

using static ImprovedHordes.Utils.Logger;
using static ImprovedHordes.Utils.Math;

namespace ImprovedHordes.Horde.Wandering
{
    public class WanderingHordeSpawner : HordeSpawner
    {
        private static readonly WanderingHordeGenerator HORDE_GENERATOR = new WanderingHordeGenerator();
        public readonly WanderingHordeManager horde;

        public WanderingHordeSpawner(WanderingHordeManager horde) : base(horde.manager, HORDE_GENERATOR)
        {
            this.horde = horde;
        }

        public override int GetGroupDistance()
        {
            return GamePrefs.GetInt(EnumGamePrefs.PartySharedKillRange) * 4;
        }

        protected override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;
            float wanderTime = 90f + this.horde.manager.Random.RandomFloat * 4f;

            if (horde.horde.feral)
            {
                commands.Add(new HordeAICommandDestinationMoving(() => group.CalculateAverageGroupPosition(true), DEST_RADIUS * 2));
                commands.Add(new HordeAICommandWander(wanderTime));
            }
            else
            {
                // Random wander to try encounter players.
                // TODO
                //bool randomWander = this.horde.manager.Random.RandomFloat >= 0.5f;

                // Is this broken?

                if (true)
                {
                    float halfwayToEnd = this.horde.manager.Random.RandomRange(0f, 0.5f);

                    Vector3 wanderPos = horde.spawnPosition + (horde.targetPosition - horde.spawnPosition) / (1 - halfwayToEnd);
                    Utils.GetSpawnableY(ref wanderPos);

                    commands.Add(new HordeAICommandDestination(wanderPos, DEST_RADIUS));
                    commands.Add(new HordeAICommandWander(wanderTime));
                }
            }

            commands.Add(new HordeAICommandDestination(GetRandomNearbyPosition(horde.targetPosition, DEST_RADIUS), DEST_RADIUS));

            AstarManager.Instance.AddLocation(entity.position, 64);
            this.horde.manager.AIManager.Add(entity, horde.horde, true, commands);
        }

        protected override void PreSpawn(PlayerHordeGroup group, SpawningHorde horde)
        {
            this.horde.hordes.Add(horde.horde);

            // TODO: Parents.
            if (horde.horde.group.parent == null && horde.horde.group.children == null)
            {
                this.horde.schedule.AddWeeklyOccurancesForGroup(group.members, horde.horde.group);
            }
            else
            {
                List<HordeGroup> children = horde.horde.group.children;

                if (horde.horde.group.parent != null)
                {
                    HordeGroup parentGroup = horde.horde.group.GetParent();

                    this.horde.schedule.AddWeeklyOccurancesForGroup(group.members, parentGroup);
                    
                    foreach(var child in parentGroup.children)
                    {
                        this.horde.schedule.AddWeeklyOccurancesForGroup(group.members, child);
                    }
                }
                else
                {
                    this.horde.schedule.AddWeeklyOccurancesForGroup(group.members, horde.horde.group);
                }

                if (children != null)
                {
                    foreach (var child in children)
                    {
                        this.horde.schedule.AddWeeklyOccurancesForGroup(group.members, child);
                    }
                }
            }

            Log("Horde for group: {0}", group.ToString());
            Log("Horde Group: {0}", horde.horde.group.name);
            Log("GS: {0}", group.GetGroupGamestage());
            Log("Start Pos: " + horde.spawnPosition.ToString());
            Log("End Pos: " + horde.targetPosition.ToString());
            Log("Horde size: " + horde.horde.count);
        }

        public void SpawnWanderingHordes()
        {
            Log("[Wandering Horde] Occurance {0} Spawning", this.horde.schedule.currentOccurance + 1);

            this.horde.state = WanderingHordeManager.EHordeState.StillAlive;
            this.StartSpawningFor(GetAllHordeGroups(), this.horde.schedule.GetCurrentOccurance().feral);
        }

        private sealed class WanderingHordeGenerator : HordeGenerator
        {
            public WanderingHordeGenerator() : base("wandering")
            { }

            public override bool CanHordeGroupBePicked(PlayerHordeGroup playerGroup, HordeGroup group, string biomeAtPosition)
            {
                WanderingHordeManager wanderingHorde = HordeManager.Instance.WanderingHorde;

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