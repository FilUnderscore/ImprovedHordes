﻿using System;
using System.Collections.Generic;
using System.Linq;

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

        public Horde GenerateHorde(PlayerHordeGroup playerGroup, bool feral)
        {
            var groups = HordesList.hordes[this.type].hordes;
            List<HordeGroup> groupsToPick = new List<HordeGroup>();

            foreach(var group in groups.Values)
            {
                if (!CanHordeGroupBePicked(playerGroup, group))
                    continue;

                groupsToPick.Add(group);
            }

            if (groupsToPick.Count == 0)
                groupsToPick.AddRange(groups.Values);

            GameRandom random = HordeManager.Instance.Random;
            HordeGroup randomGroup = groupsToPick[random.RandomRange(0, groupsToPick.Count)];
            Dictionary<HordeGroupEntity, int> entitiesToSpawn = new Dictionary<HordeGroupEntity, int>();

            int gamestage = playerGroup.GetGroupGamestage();
            EvaluateEntitiesInGroup(randomGroup, ref entitiesToSpawn, gamestage);

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

            return new Horde(playerGroup, randomGroup, totalCount, feral, entityIds);
        }

        public virtual bool CanHordeGroupBePicked(PlayerHordeGroup playerGroup, HordeGroup group)
        {
            int gamestage = playerGroup.GetGroupGamestage();

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
                return false;

            return true;
        }

        private void EvaluateEntitiesInGroup(HordeGroup randomGroup, ref Dictionary<HordeGroupEntity, int> entitiesToSpawn, int gamestage)
        {
            GameRandom random = HordeManager.Instance.Random;

            foreach (var entity in randomGroup.entities)
            {
                if (entity.chance != null && entity.chance.Evaluate() < random.RandomFloat)
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

                        if (countDecGS > 0 && gamestage > countDecGS && gs.countDecPerPostGS != null)
                        {
                            float countDecPerPostGS = gs.countDecPerPostGS.Evaluate();

                            int decGSSpawn = (int)Math.Floor(countDecPerPostGS * (gamestage - countDecGS));

                            if (decGSSpawn > 0)
                                toSpawn -= decGSSpawn;
                        }

                        if (toSpawn < 0)
                            toSpawn = 0;

                        count = toSpawn;
                    }

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
                        EvaluateEntitiesInGroup(randomHordeGroup, ref entitiesToSpawn, gamestage);
                    }
                    else
                    {
                        if (!HordesList.hordes[entity.horde].hordes.ContainsKey(entity.group))
                        {
                            throw new NullReferenceException($"Horde type {entity.group} does not exist in horde type {entity.horde}.");
                        }

                        var subGroup = HordesList.hordes[entity.horde].hordes[entity.group];
                        EvaluateEntitiesInGroup(subGroup, ref entitiesToSpawn, gamestage);
                    }
                }
            }
        }
    }
}
