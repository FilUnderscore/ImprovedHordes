using ImprovedHordes.Core.Abstractions.Random;
using ImprovedHordes.Core.World.Horde;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Data.XML
{
    public sealed class HordeDefinition
    {
        private readonly string type;
        private readonly float totalWeight;

        internal readonly List<HordeDefinition> merge = new List<HordeDefinition>();
        private readonly List<Group> groups = new List<Group>();

        public HordeDefinition(string type, XmlEntry entry)
        {
            this.type = type;

            float totalWeight = 0.0f;

            entry.GetEntries("groups")[0].GetEntries("group").ForEach(groupEntry =>
            {
                Group group = new Group(groupEntry, totalWeight);
                totalWeight += group.GetWeight();
                this.groups.Add(group);
            });

            this.totalWeight = totalWeight;
        }

        public string GetHordeType()
        {
            return this.type;
        }

        public bool CanMergeWith(HordeDefinition other)
        {
            return merge.Contains(other);
        }

        public Group GetEligibleRandomGroup(PlayerHordeGroup playerGroup, IRandom random)
        {
            List<Group> eligibleGroups = groups.Where(group => group.IsEligible(playerGroup, random, this)).ToList();

            if (eligibleGroups == null || eligibleGroups.Count == 0) // Try find eligible groups again but ignore weight.
                eligibleGroups = groups.Where(group => group.IsEligible(playerGroup, null, null)).ToList();

            if (eligibleGroups == null || eligibleGroups.Count == 0)
                return null;

            return random.Random<Group>(eligibleGroups);
        }

        public bool IsWithinWeightRange(Group group, float randomNormalizedWeight)
        {
            float weight = randomNormalizedWeight * this.totalWeight;
            return group.IsWithinWeightRange(weight);
        }

        public sealed class Group
        {
            private readonly float weightStart, weight;
            private readonly List<Entity> entities = new List<Entity>();
            
            public Group(XmlEntry entry, float weightStart)
            {
                this.weightStart = weightStart;

                if (entry.GetAttribute("weight", out string weightValue))
                    this.weight = Mathf.Max(0.0f, float.Parse(weightValue));
                else
                    this.weight = 1.0f;

                entry.GetEntries("entity").ForEach(entityEntry =>
                {
                    this.entities.Add(new Entity(entityEntry));
                });

                entry.GetEntries("gs").ForEach(gsEntry =>
                {
                    GS gs = new GS(gsEntry);

                    this.entities.AddRange(gs.GetEntities());
                });
            }

            public float GetWeight()
            {
                return this.weight;
            }

            public bool IsWithinWeightRange(float weight)
            {
                return weight >= this.weightStart && weight < this.weightStart + this.weight;
            }

            public bool IsEligible(PlayerHordeGroup playerGroup, IRandom random, HordeDefinition definition)
            {
                return this.entities.Where(entity => entity.IsEligible(playerGroup, random)).Any() && 
                    (random == null || definition == null || definition.IsWithinWeightRange(this, random.RandomFloat));
            }

            public List<Entity> GetEligible(PlayerHordeGroup playerGroup, IRandom random)
            {
                return this.entities.Where(entity => entity.IsEligible(playerGroup, random)).ToList();
            }

            public sealed class GS
            {
                private readonly int min;
                private readonly int? max;
                private readonly float? increaseEvery;

                private readonly List<Entity> entities = new List<Entity>();

                public GS(XmlEntry entry)
                {
                    if(entry.GetAttribute("min", out string minValue))
                        this.min = Math.Max(0, int.Parse(minValue));

                    if(entry.GetAttribute("max", out string maxValue))
                        this.max = Math.Max(0, int.Parse(maxValue));
                    else if(entry.GetAttribute("increaseEvery", out string increaseEvery))
                        this.increaseEvery = Mathf.Max(0.0f, float.Parse(increaseEvery));

                    entry.GetEntries("entity").ForEach(entityEntry =>
                    {
                        this.entities.Add(new Entity(this, entityEntry));
                    });

                    entry.GetEntries("gs").ForEach(gsEntry =>
                    {
                        GS gs = new GS(gsEntry);

                        this.entities.AddRange(gs.GetEntities());
                    });
                }

                public int GetCount(int gs, int minCount, int maxCount)
                {
                    if (this.max == null && this.increaseEvery != null && this.increaseEvery > 0.0f)
                    {
                        return GetCountRelativeToMinIncrease(gs, minCount, maxCount);
                    }
                    else if(this.max != null && this.max > 0 && this.increaseEvery == null)
                    {
                        return GetCountRelativeToMinMax(gs, minCount, maxCount);
                    }
                    else
                    {
                        return GetCountRandom(minCount, maxCount);
                    }
                }

                private int GetCountRelativeToMinIncrease(int gs, int minCount, int maxCount)
                {
                    return Mathf.Clamp(minCount + Mathf.RoundToInt((1f / this.increaseEvery.Value) * (gs - min)), minCount, maxCount);
                }

                private int GetCountRandom(int minCount, int maxCount)
                {
                    return GameManager.Instance.World.GetGameRandom().RandomRange(maxCount - minCount) + minCount;
                }

                private int GetCountRelativeToMinMax(int gs, int minCount, int maxCount)
                {
                    float pct = (gs - min) / (float)max;

                    if (pct <= 0.33f)
                    {
                        pct *= 3f;
                    }
                    else if (pct > 0.33f && pct <= 0.66f)
                    {
                        pct = (1f - (pct - 0.33f) * 1.5f);
                    }
                    else
                    {
                        pct = (0.5f - (pct - 0.66f) * 1.5f);
                    }

                    return Mathf.RoundToInt((maxCount - minCount) * pct + minCount);
                }

                public List<Entity> GetEntities()
                {
                    return this.entities;
                }

                public bool IsEligible(PlayerHordeGroup playerGroup)
                {
                    int groupGS = playerGroup.GetGamestage();

                    if(this.max != null && groupGS >= this.max)
                        return false;

                    return groupGS >= this.min;
                }
            }

            public enum TimeOfDay
            {
                Day,
                Night,
                Both
            }

            private static TimeOfDay TimeOfDayFromString(string str)
            {
                if(str.EqualsCaseInsensitive("day"))
                {
                    return TimeOfDay.Day;
                }
                else if(str.EqualsCaseInsensitive("night"))
                {
                    return TimeOfDay.Night;
                }

                return TimeOfDay.Both;
            }

            private static bool IsTimeOfDay(TimeOfDay timeOfDay)
            {
                bool isDay = GameManager.Instance.World.IsDaytime();

                switch(timeOfDay)
                {
                    case TimeOfDay.Day:
                        return isDay;
                    case TimeOfDay.Night:
                        return !isDay;
                    case TimeOfDay.Both:
                    default:
                        return true;
                }
            }

            public sealed class Entity
            {
                private readonly GS gs;
                private readonly TimeOfDay time;
                private readonly string[] biomes;

                private readonly int entityClass;
                private readonly string entityGroup;

                private readonly int minCount, maxCount;
                private readonly float chance;

                public Entity(XmlEntry entry) : this(null, entry) { }

                public Entity(GS gs, XmlEntry entry)
                {
                    this.gs = gs;

                    if (entry.GetAttribute("time", out string timeValue))
                        this.time = TimeOfDayFromString(timeValue);
                    else
                        this.time = TimeOfDay.Both;

                    if (entry.GetAttribute("biomes", out string biomesValue))
                        this.biomes = biomesValue.Split(',');

                    if (entry.GetAttribute("name", out string nameValue))
                        this.entityClass = EntityClass.FromString(nameValue);
                    else if (entry.GetAttribute("group", out string groupValue))
                        this.entityGroup = groupValue;

                    if (entry.GetAttribute("minCount", out string minCountValue))
                        this.minCount = Math.Max(0, int.Parse(minCountValue));

                    if(entry.GetAttribute("maxCount", out string maxCountValue))
                        this.maxCount = Math.Max(0, int.Parse(maxCountValue));

                    if (entry.GetAttribute("chance", out string chanceValue))
                        this.chance = Mathf.Max(0.0f, float.Parse(chanceValue));
                    else
                        this.chance = 1.0f;
                }

                public int GetCount(int gs)
                {
                    if(this.gs == null)
                    {
                        return GameManager.Instance.World.GetGameRandom().RandomRange(this.maxCount - this.minCount + 1) + this.minCount;
                    }

                    return this.gs.GetCount(gs, minCount, maxCount);
                }

                public bool IsEligible(PlayerHordeGroup playerGroup, IRandom random)
                {
                    return (this.gs == null || this.gs.IsEligible(playerGroup)) &&
                        IsTimeOfDay(this.time) &&
                        (this.biomes == null || this.biomes.Contains(playerGroup.GetBiome()) &&
                        (random == null || random.RandomChance(this.chance)));
                }

                public bool GetEntityClassId(ref int lastEntityClassId, out int entityClassId, GameRandom random)
                {
                    if (this.entityGroup == null)
                    {
                        entityClassId = this.entityClass;
                        return true;
                    }

                    if(!EntityGroups.list.TryGetValue(this.entityGroup, out var list) || list == null || list.Count == 0)
                    {
                        entityClassId = 0;
                        return false;
                    }

                    entityClassId = EntityGroups.GetRandomFromGroup(this.entityGroup, ref lastEntityClassId, random);
                    return true;
                }
            }
        }
    }
}
