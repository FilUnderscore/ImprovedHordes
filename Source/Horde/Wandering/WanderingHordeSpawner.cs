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

        public override void OnSpawn(EntityAlive entity, PlayerHordeGroup group, SpawningHorde horde)
        {
            List<HordeAICommand> commands = new List<HordeAICommand>();
            const int DEST_RADIUS = 10;

            if (horde.horde.feral)
            {
                commands.Add(new HordeAICommandDestinationMoving(() => CalculateAverageGroupPosition(group), DEST_RADIUS * 2));
                commands.Add(new HordeAICommandWander(50f));
            }

            commands.Add(new HordeAICommandDestination(GetRandomNearbyPosition(horde.targetPosition, DEST_RADIUS), DEST_RADIUS));

            this.horde.manager.AIManager.Add(entity, horde.horde, true, commands);
        }

        public override bool GetSpawnPosition(PlayerHordeGroup group, out Vector3 spawnPosition, out Vector3 targetPosition)
        {
            var averageGroupPosition = CalculateAverageGroupPosition(group);

            return CalculateWanderingHordePositions(averageGroupPosition, out spawnPosition, out targetPosition);
        }

        public override void PreSpawn(PlayerHordeGroup group, SpawningHorde horde)
        {
            this.horde.hordes.Add(horde.horde);
            this.horde.schedule.AddWeeklyOccurancesForGroup(group.members, horde.horde.group);

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
            this.StartSpawningFor(GetAllHordeGroups());
        }

        private Vector3 CalculateAverageGroupPosition(PlayerHordeGroup playerHordeGroup)
        {
            List<EntityPlayer> players = playerHordeGroup.members;

            Vector3 avg = Vector3.zero;

            foreach (var player in players)
            {
                avg += player.position;
            }

            avg /= players.Count;

            if (!GetSpawnableY(ref avg))
            {
                // Testing this.
                Error("Failed to get spawnable Y.");
            }

            return avg;
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
            var result = GetSpawnableY(ref endPos);

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

        public Vector3 GetSpawnableCircleFromPos(Vector3 playerPos, float radius)
        {
            Vector2 startCircle = this.horde.manager.Random.RandomOnUnitCircle;

            float x = (startCircle.x * radius) + playerPos.x;
            float z = (startCircle.y * radius) + playerPos.z;

            Vector3 circleFromPlayer = new Vector3(x, 0, z);
            bool result = GetSpawnableY(ref circleFromPlayer);

            if (!result)
            {
                Log("[Wandering Horde] Failed to find spawnable circle from pos. X" + x + " Z " + z);
                return GetSpawnableCircleFromPos(playerPos, radius);
            }

            return circleFromPlayer;
        }

        private sealed class WanderingHordeGenerator : HordeGenerator
        {
            public WanderingHordeGenerator() : base("wandering")
            { }

            public override Horde GenerateHorde(PlayerHordeGroup playerHordeGroup)
            {
                int gamestage = playerHordeGroup.GetGroupGamestage();

                var wanderingHorde = HordeManager.Instance.WanderingHorde;
                var occurance = wanderingHorde.schedule.occurances[wanderingHorde.schedule.currentOccurance];

                var groups = Hordes.hordes[this.type].Values;
                List<HordeGroup> groupsToPick = new List<HordeGroup>();

                foreach (var group in groups)
                {
                    if (group.MaxWeeklyOccurances != null)
                    {
                        var maxWeeklyOccurances = group.MaxWeeklyOccurances.Evaluate();

                        var weeklyOccurancesForPlayer = wanderingHorde.schedule.GetAverageWeeklyOccurancesForGroup(playerHordeGroup, group);

                        if (weeklyOccurancesForPlayer >= maxWeeklyOccurances)
                            continue;

                        if (maxWeeklyOccurances > 0)
                        {
                            float diminishedChance = (float)Math.Pow(1 / maxWeeklyOccurances, weeklyOccurancesForPlayer);

                            if (wanderingHorde.manager.Random.RandomFloat > diminishedChance)
                                continue;
                        }
                    }

                    if (group.PrefWeekDays != null)
                    {
                        var prefWeekDays = group.PrefWeekDays.Evaluate();
                        var weekDay = wanderingHorde.GetCurrentWeekDay();

                        // RNG whether to still spawn this horde, adds variation.
                        bool randomChance = wanderingHorde.manager.Random.RandomFloat >= 0.5f;

                        if (!randomChance && !prefWeekDays.Contains(weekDay))
                            continue;
                    }

                    int groupsThatMatchGS = 0;
                    foreach (var entities in group.entities)
                    {
                        if (entities.gs != null)
                        {
                            GS gs = entities.gs;

                            if (gs.min != null && gamestage < gs.min.Evaluate())
                                continue;

                            if (gs.max != null && gamestage > gs.max.Evaluate())
                                continue;
                        }

                        groupsThatMatchGS++;
                    }

                    if (groupsThatMatchGS == 0)
                        continue;

                    groupsToPick.Add(group);
                }

                if (groupsToPick.Count == 0)
                    groupsToPick.AddRange(groups);

                HordeGroup randomGroup = groupsToPick[wanderingHorde.manager.Random.RandomRange(0, groupsToPick.Count)];
                Dictionary<HordeGroupEntity, int> entitiesToSpawn = new Dictionary<HordeGroupEntity, int>();

                EvaluateEntitiesInGroup(randomGroup, entitiesToSpawn, wanderingHorde, gamestage);

                // Add entities to list.
                List<int> entityIds = new List<int>();
                int totalCount = 0;

                foreach (var entitySet in entitiesToSpawn)
                {
                    HordeGroupEntity ent = entitySet.Key;
                    int count = entitySet.Value;

                    if (ent.name != null)
                    {
                        int entityId = EntityClass.FromString(ent.name);

                        for (var i = 0; i < count; i++)
                            entityIds.Add(entityId);

                        totalCount += count;
                    }
                    else if (ent.group != null)
                    {
                        int lastEntityId = -1;

                        for (var i = 0; i < count; i++)
                        {
                            int entityId = EntityGroups.GetRandomFromGroup(ent.group, ref lastEntityId, wanderingHorde.manager.Random);

                            entityIds.Add(entityId);
                        }

                        totalCount += count;
                    }
                    else
                    {
                        Error("[Wandering Horde] Horde entity in group {0} has no name or group. Skipping.", randomGroup.name);
                        continue;
                    }
                }

                // Randomize entities.
                entityIds.Randomize();

                return new Horde(playerHordeGroup, randomGroup, totalCount, occurance.feral, entityIds);
            }

            private void EvaluateEntitiesInGroup(HordeGroup randomGroup, Dictionary<HordeGroupEntity, int> entitiesToSpawn, WanderingHorde wanderingHorde, int gamestage)
            {
                foreach (var entity in randomGroup.entities)
                {
                    if (entity.chance != null && entity.chance.Evaluate() < wanderingHorde.manager.Random.RandomFloat)
                        continue;

                    entitiesToSpawn.Add(entity, 0);

                    int minCount = entity.minCount != null ? entity.minCount.Evaluate() : 0;
                    int maxCount = entity.maxCount != null ? entity.maxCount.Evaluate() : -1;

                    GS gs = entity.gs;
                    int minGS = gs != null && gs.min != null ? gs.min.Evaluate() : 0;
                    int maxGS = gs != null && gs.max != null ? gs.max.Evaluate() : -1;

                    if (gs != null) // Keep an eye on.
                    {
                        if (gamestage < minGS)
                            continue;

                        if (maxGS > 0 && gamestage > maxGS)
                            continue;
                    }

                    int count;

                    if (entity.horde == null)
                    {
                        if (gs == null || gs.countIncPerGS == null)
                        {
                            if (maxCount > 0)
                                count = wanderingHorde.manager.Random.RandomRange(minCount, maxCount + 1);
                            else
                            {
                                Error("Cannot calculate count of entity/entitygroup {0} in group {1} because no gamestage or maximum count has been specified.", entity.name ?? entity.group, randomGroup.name);
                                count = 0;
                            }
                        }
                        else
                        {
                            float countIncPerGS = gs.countIncPerGS.Evaluate();

                            int toSpawn = minCount + (int)Math.Floor(countIncPerGS * (gamestage - minGS));
                            int countDecGS = gs.countDecGS != null ? gs.countDecGS.Evaluate() : 0;

                            Log("{0} {1} {2}", gs.countDecGS == null, gs.countDecPerPostGS == null, countDecGS);
                            if (countDecGS > 0 && gamestage > countDecGS && gs.countDecPerPostGS != null)
                            {
                                float countDecPerPostGS = gs.countDecPerPostGS.Evaluate();

                                int decGSSpawn = (int)Math.Floor(countDecPerPostGS * (gamestage - countDecGS));
                                Log("Dec GS Spawn {0}", decGSSpawn);

                                if (decGSSpawn > 0)
                                    toSpawn -= decGSSpawn;
                            }

                            // TODO.
                            if (toSpawn < 0)
                                toSpawn = 0;

                            count = toSpawn;
                        }

                        if (maxCount >= 0 && count > maxCount)
                            count = maxCount;

                        entitiesToSpawn[entity] += count;

#if DEBUG
                        Log("[Wandering Horde] Spawning {0} of {1}", count, entity.name ?? entity.group);
#endif
                    }
                    else
                    {
                        if (!Hordes.hordes.ContainsKey(entity.horde))
                        {
                            throw new NullReferenceException($"No such horde with type {entity.horde} exists.");
                        }

                        if (entity.group == null)
                        {
                            var hordesDict = Hordes.hordes[entity.horde];

                            var randomHordeGroup = hordesDict.Values.ToList().ElementAt(wanderingHorde.manager.Random.RandomRange(0, hordesDict.Count));
                            EvaluateEntitiesInGroup(randomHordeGroup, entitiesToSpawn, wanderingHorde, gamestage);
                        }
                        else
                        {
                            if(!Hordes.hordes[entity.horde].ContainsKey(entity.group))
                            {
                                throw new NullReferenceException($"Horde type {entity.group} does not exist in horde type {entity.horde}.");
                            }

                            var subGroup = Hordes.hordes[entity.horde][entity.group];
                            EvaluateEntitiesInGroup(subGroup, entitiesToSpawn, wanderingHorde, gamestage);
                        }
                    }
                }
            }
        }
    }
}