using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using ImprovedHordes.Horde.Data;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde
{
    public abstract class HordeGenerator
    {
        protected string type;
        public HordeGenerator(string type)
        {
            this.type = type;
        }

        public bool GenerateHorde(PlayerHordeGroup playerGroup, bool feral, out Horde horde)
        {
            var groups = HordesList.hordes[this.type].hordes;
            List<HordeGroup> groupsToPick = new List<HordeGroup>();

            Vector3 groupPosition = playerGroup.CalculateAverageGroupPosition(false);
            string biomeAtPosition = ImprovedHordesManager.Instance.World.GetBiome(global::Utils.Fastfloor(groupPosition.x), global::Utils.Fastfloor(groupPosition.z)).m_sBiomeName;

            foreach (var group in groups.Values)
            {
                if (!CanHordeGroupBePicked(playerGroup, group, biomeAtPosition))
                    continue;

                groupsToPick.Add(group);
            }

            if (groupsToPick.Count == 0)
            {
                // No groups to select.
                horde = null;
                return false;
            }

            GameRandom random = ImprovedHordesManager.Instance.Random;
            HordeGroup randomGroup = RandomGroup(groupsToPick, random);
            Dictionary<HordeGroupEntity, int> entitiesToSpawn = new Dictionary<HordeGroupEntity, int>();

            int gamestage = playerGroup.GetGroupGamestage();
            EvaluateEntitiesInGroup(randomGroup, ref entitiesToSpawn, gamestage, biomeAtPosition);

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
                        int entityId = EntityGroups.GetRandomFromGroup(ent.group, ref lastEntityId, random);

                        entityIds.Add(entityId);
                    }

                    totalCount += count;
                }
                else
                {
                    Error("[{0}] Horde entity in group {1} has no name or group. Skipping.", this.GetType().FullName, randomGroup.name);
                    continue;
                }
            }

            entityIds.Randomize();

            horde = new Horde(playerGroup, randomGroup, totalCount, feral, entityIds);
            return true;
        }

        private HordeGroup RandomGroup(List<HordeGroup> groups, GameRandom random)
        {
            HordeGroup pickedGroup = null;

            float totalWeight = 0.0f;
            Dictionary<HordeGroup, WeightRange> weightedGroups = new Dictionary<HordeGroup, WeightRange>();

            foreach(var group in groups)
            {
                float weight = group.Weight != null ? group.Weight.Evaluate() : 1.0f;
                
                if(group.PrefWeekDays != null)
                {
                    HashSet<int> prefDays = group.PrefWeekDays.Evaluate();

                    if(prefDays.Contains(RuntimeEval.Registry.GetVariable<int>("weekDay")))
                    {
                        weight += (weight < 1.0 ? 1 / weight : weight) * prefDays.Count;
                    }
                }

                weightedGroups.Add(group, new WeightRange
                {
                    Min = totalWeight,
                    Max = totalWeight + weight
                });

                totalWeight += weight;
            }

            float pickedWeight = random.RandomRange(totalWeight);

            foreach(var groupEntry in weightedGroups)
            {
                var group = groupEntry.Key;
                var weights = groupEntry.Value;

                if(weights.InRange(pickedWeight))
                {
                    pickedGroup = group;
                    break;
                }
            }

            return pickedGroup;
        }

        struct WeightRange
        {
            public float Min;
            public float Max;

            public bool InRange(float value)
            {
                return value >= Min && value < Max;
            }
        }

        public virtual bool CanHordeGroupBePicked(PlayerHordeGroup playerGroup, HordeGroup group, string biomeAtPosition)
        {
            int gamestage = playerGroup.GetGroupGamestage();
            bool isDay = ImprovedHordesManager.Instance.World.IsDaytime();

            int groupsThatMatchGS = 0;
            foreach (var entity in group.entities)
            {
                if(entity.biomes != null)
                {
                    // Biome specific spawns.
                    HashSet<string> biomes = entity.biomes.Evaluate();

                    if(!biomes.Contains(biomeAtPosition))
                    {
                        continue;
                    }
                }

                if(entity.timeOfDay != null)
                {
                    // Time of day specific spawns.
                    ETimeOfDay timeOfDay = entity.timeOfDay.Evaluate();

                    if ((timeOfDay == ETimeOfDay.Night && isDay) || (timeOfDay == ETimeOfDay.Day && !isDay))
                        continue;
                }

                if (entity.gs != null)
                {
                    GS gs = entity.gs;

                    if (gs.min != null && gamestage < gs.min.Evaluate())
                        continue;

                    if (gs.max != null && gamestage >= gs.max.Evaluate())
                        continue;
                }

                groupsThatMatchGS++;
            }

            if (groupsThatMatchGS == 0)
                return false;

            return true;
        }

        private void EvaluateEntitiesInGroup(HordeGroup randomGroup, ref Dictionary<HordeGroupEntity, int> entitiesToSpawn, int gamestage, string biomeAtPosition)
        {
            GameRandom random = ImprovedHordesManager.Instance.Random;
            bool isDay = ImprovedHordesManager.Instance.World.IsDaytime();

            foreach (var entity in randomGroup.entities)
            {
                if (entity.biomes != null)
                {
                    // Biome specific spawns.
                    HashSet<string> biomes = entity.biomes.Evaluate();

                    if (!biomes.Contains(biomeAtPosition))
                    {
                        continue;
                    }
                }

                if (entity.timeOfDay != null)
                {
                    // Time of day specific spawns.
                    ETimeOfDay timeOfDay = entity.timeOfDay.Evaluate();

                    if ((timeOfDay == ETimeOfDay.Night && isDay) || (timeOfDay == ETimeOfDay.Day && !isDay))
                        continue;
                }

                if (entity.chance != null && entity.chance.Evaluate() < random.RandomFloat)
                    continue;

                int minCount = entity.minCount != null ? entity.minCount.Evaluate() : 0;
                int maxCount = entity.maxCount != null ? entity.maxCount.Evaluate() : -1;

                GS gs = entity.gs;
                int minGS = gs != null && gs.min != null ? gs.min.Evaluate() : 0;
                int maxGS = gs != null && gs.max != null ? gs.max.Evaluate() : -1;

                if (gs != null) // Keep an eye on.
                {
                    if (gamestage < minGS)
                        continue;

                    if (maxGS > 0 && gamestage >= maxGS)
                        continue;
                }

                int count;

                if (entity.horde == null)
                {
                    if (gs == null || gs.countIncPerGS == null)
                    {
                        if (maxCount > 0)
                            count = random.RandomRange(minCount, maxCount + 1);
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

                        if (maxCount >= 0 && toSpawn > maxCount)
                            toSpawn = maxCount;

                        if (countDecGS > 0 && gamestage > countDecGS)
                        {
                            float countDecPerPostGS;

                            if (gs.countDecPerPostGS != null)
                                countDecPerPostGS = gs.countDecPerPostGS.Evaluate();
                            else if (gs.countDecPerPostGS == null && gs.max != null)
                                countDecPerPostGS = toSpawn / (maxGS - countDecGS);
                            else
                            {
                                Error("[{0}] Unable to calculate entity decrease after GS {1} for entity {2} in group {3}.", this.GetType().FullName, countDecGS, entity.name ?? entity.group, randomGroup.name);
                                countDecPerPostGS = 0.0f;
                            }

                            int decGSSpawn = (int)Math.Floor(countDecPerPostGS * (gamestage - countDecGS));

                            if (decGSSpawn > 0)
                                toSpawn -= decGSSpawn;
                        }

                        if (toSpawn < 0)
                            toSpawn = 0;

                        count = toSpawn;
                    }

                    if (!entitiesToSpawn.ContainsKey(entity))
                        entitiesToSpawn.Add(entity, 0);

                    entitiesToSpawn[entity] += count;

#if DEBUG
                    Log("[{0}] Spawning {1} of {2}", this.GetType().FullName, count, entity.name ?? entity.group);
#endif
                }
                else
                {
                    if (!HordesList.hordes.ContainsKey(entity.horde))
                    {
                        throw new NullReferenceException($"No such horde with type {entity.horde} exists.");
                    }

                    if (entity.group == null)
                    {
                        var hordesDict = HordesList.hordes[entity.horde].hordes;

                        var randomHordeGroup = hordesDict.Values.ToList().ElementAt(random.RandomRange(0, hordesDict.Count));
                        EvaluateEntitiesInGroup(randomHordeGroup, ref entitiesToSpawn, gamestage, biomeAtPosition);
                    }
                    else
                    {
                        if (!HordesList.hordes[entity.horde].hordes.ContainsKey(entity.group))
                        {
                            throw new NullReferenceException($"Horde type {entity.group} does not exist in horde type {entity.horde}.");
                        }

                        var subGroup = HordesList.hordes[entity.horde].hordes[entity.group];
                        EvaluateEntitiesInGroup(subGroup, ref entitiesToSpawn, gamestage, biomeAtPosition);
                    }
                }
            }
        }
    }
}
