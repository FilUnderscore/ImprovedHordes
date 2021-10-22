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
        public readonly WanderingHorde horde;

        public WanderingHordeSpawner(WanderingHorde horde) : base(HORDE_GENERATOR)
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
                commands.Add(new HordeAICommandDestinationMoving(() => CalculateAverageGroupPosition(group), DEST_RADIUS * 2));
                commands.Add(new HordeAICommandWander(wanderTime));
            }
            else
            {
                // Random wander to try encounter players.
                // TODO
                //bool randomWander = this.horde.manager.Random.RandomFloat >= 0.5f;

                if (true)
                {
                    float halfwayToEnd = this.horde.manager.Random.RandomRange(0f, 0.5f);

                    Vector3 wanderPos = horde.spawnPosition + (horde.targetPosition - horde.spawnPosition) / (1 - halfwayToEnd);

                    if (Utils.GetSpawnableY(ref wanderPos))
                    {
                        commands.Add(new HordeAICommandDestination(wanderPos, DEST_RADIUS));
                        commands.Add(new HordeAICommandWander(wanderTime));
                    }
                }
            }

            commands.Add(new HordeAICommandDestination(GetRandomNearbyPosition(horde.targetPosition, DEST_RADIUS), DEST_RADIUS));

            AstarManager.Instance.AddLocation(entity.position, 64);
            this.horde.manager.AIManager.Add(entity, horde.horde, true, commands);
        }

        public override bool GetSpawnPosition(PlayerHordeGroup group, out Vector3 spawnPosition, out Vector3 targetPosition)
        {
            var averageGroupPosition = CalculateAverageGroupPosition(group);

            return CalculateWanderingHordePositions(averageGroupPosition, out spawnPosition, out targetPosition);
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

            this.horde.state = WanderingHorde.EHordeState.StillAlive;
            this.StartSpawningFor(GetAllHordeGroups(), this.horde.GetCurrentOccurance().feral);
        }

        

        private Vector3 GetRandomNearbyPosition(Vector3 target, float radius)
        {
            Vector2 random = this.horde.manager.Random.RandomOnUnitCircle;

            float x = target.x + random.x * radius;
            float z = target.z + random.y * radius;

            return new Vector3(x, target.y, z);
        }

        public bool CalculateWanderingHordePositions(Vector3 commonPos, out Vector3 startPos, out Vector3 endPos)
        {
            var random = this.horde.manager.Random;

            var radius = random.RandomRange(80, 12 * GamePrefs.GetInt(EnumGamePrefs.ServerMaxAllowedViewDistance)); // TODO: Make XML setting.
            startPos = GetSpawnableCircleFromPos(commonPos, radius);

            this.horde.manager.World.GetRandomSpawnPositionMinMaxToPosition(commonPos, 20, 40, 20, true, out Vector3 randomPos);

            var intersections = FindLineCircleIntersections(randomPos.x, randomPos.z, radius, startPos, commonPos, out _, out Vector2 intEndPos);

            endPos = new Vector3(intEndPos.x, 0, intEndPos.y);
            var result = Utils.GetSpawnableY(ref endPos);

            if (!result)
            {
                return CalculateWanderingHordePositions(commonPos, out startPos, out endPos);
            }

            if (intersections < 2)
            {
                Warning("[Wandering Horde] Only 1 intersection was found.");

                return false;
            }

            return true;
        }

        public Vector3 GetSpawnableCircleFromPos(Vector3 playerPos, float radius, int attempt = 0)
        {
            Vector2 startCircle = this.horde.manager.Random.RandomOnUnitCircle;

            float x = (startCircle.x * radius) + playerPos.x;
            float z = (startCircle.y * radius) + playerPos.z;

            Vector3 circleFromPlayer = new Vector3(x, 0, z);
            bool result = Utils.GetSpawnableY(ref circleFromPlayer);

            if (!result)
            {
                Log("[Wandering Horde] Failed to find spawnable circle from pos. X" + x + " Z " + z);

                if(attempt < 10)
                    return GetSpawnableCircleFromPos(playerPos, radius, attempt++);
                else
                {
                    if (this.horde.manager.World.GetRandomSpawnPositionMinMaxToPosition(playerPos, 20, (int)radius, 20, true, out Vector3 alt))
                        return alt;

                    throw new InvalidOperationException($"Failed to find a spawnable location near {playerPos.ToString()}");
                }
            }

            return circleFromPlayer;
        }

        private sealed class WanderingHordeGenerator : HordeGenerator
        {
            public WanderingHordeGenerator() : base("wandering")
            { }

            public override bool CanHordeGroupBePicked(PlayerHordeGroup playerGroup, HordeGroup group)
            {
                WanderingHorde wanderingHorde = HordeManager.Instance.WanderingHorde;

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

                if (group.PrefWeekDays != null)
                {
                    var prefWeekDays = group.PrefWeekDays.Evaluate();
                    var weekDay = wanderingHorde.GetCurrentWeekDay();

                    // RNG whether to still spawn this horde, adds variation.
                    bool randomChance = wanderingHorde.manager.Random.RandomFloat >= 0.5f;

                    if (!randomChance && !prefWeekDays.Contains(weekDay))
                        return false;
                }

                return base.CanHordeGroupBePicked(playerGroup, group);
            }
        }
    }
}