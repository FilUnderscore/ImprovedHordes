using ImprovedHordes.Source.Core.Horde.World;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ImprovedHordes.Source.Core.Horde.Data.XML
{
    public sealed class HordeDefinition
    {
        private readonly List<Group> groups = new List<Group>();

        public HordeDefinition(XmlEntry entry)
        {
            entry.GetEntries("groups")[0].GetEntries("group").ForEach(groupEntry =>
            {
                this.groups.Add(new Group(groupEntry));
            });
        }

        public Group GetEligibleRandomGroup(PlayerHordeGroup playerGroup)
        {
            IEnumerable<Group> eligibleGroups = groups.Where(group => group.IsEligible(playerGroup));

            if (eligibleGroups.Count() == 0)
                return null;

            return eligibleGroups.ToList().RandomObject();
        }

        public sealed class Group
        {
            private readonly float? chance;
            private readonly List<Entity> entities = new List<Entity>();
            
            public Group(XmlEntry entry)
            {
                if(entry.GetAttribute("chance", out string chanceValue))
                    this.chance = float.Parse(chanceValue);

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

            public bool IsEligible(PlayerHordeGroup playerGroup, bool ignoreRandom = false)
            {
                return this.entities.Where(entity => entity.IsEligible(playerGroup)).Any() && 
                    (ignoreRandom || chance == null || GameManager.Instance.World.GetGameRandom().RandomFloat <= this.chance);
            }

            public List<Entity> GetEligible(PlayerHordeGroup playerGroup)
            {
                return this.entities.Where(entity => entity.IsEligible(playerGroup)).ToList();
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
                        this.min = int.Parse(minValue);

                    if(entry.GetAttribute("max", out string maxValue))
                        this.max = int.Parse(maxValue);
                    else if(entry.GetAttribute("increaseEvery", out string increaseEvery))
                        this.increaseEvery = float.Parse(increaseEvery);

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

                public GS(int min, int max)
                {
                    this.min = min;
                    this.max = max;
                }

                public int GetCount(int gs, int minCount, int maxCount)
                {
                    if (this.max == null && this.increaseEvery != null)
                    {
                        return GetCountRelativeToMinIncrease(gs, minCount, maxCount);
                    }
                    else if(this.max != null && this.increaseEvery == null)
                    {
                        return GetCountRelativeToMinMax(gs, minCount, maxCount);
                    }

                    throw new Exception("[Improved Hordes] GS must have either a defined max attribute or increaseEvery attribute.");
                }

                private int GetCountRelativeToMinIncrease(int gs, int minCount, int maxCount)
                {
                    return Mathf.Clamp(minCount + Mathf.RoundToInt((1f / this.increaseEvery.Value) * (gs - min)), minCount, maxCount);
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

                    return groupGS >= this.min && groupGS < this.max;
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
                        this.minCount = int.Parse(minCountValue);

                    if(entry.GetAttribute("maxCount", out string maxCountValue))
                        this.maxCount = int.Parse(maxCountValue);
                }

                public int GetCount(int gs)
                {
                    if(this.gs == null)
                    {
                        return GameManager.Instance.World.GetGameRandom().RandomRange(this.maxCount - this.minCount + 1) + this.minCount;
                    }

                    return this.gs.GetCount(gs, minCount, maxCount);
                }

                public bool IsEligible(PlayerHordeGroup playerGroup)
                {
                    return (this.gs == null || this.gs.IsEligible(playerGroup)) &&
                        IsTimeOfDay(this.time) &&
                        (this.biomes == null || this.biomes.Contains(playerGroup.GetBiome()));
                }

                public int GetEntityId(ref int lastEntityId)
                {
                    if (this.entityGroup == null)
                        return this.entityClass;

                    return EntityGroups.GetRandomFromGroup(this.entityGroup, ref lastEntityId);
                }
            }
        }
    }
}
