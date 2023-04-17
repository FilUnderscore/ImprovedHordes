using ImprovedHordes.Source.Core.Horde.World;
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
            return groups.Where(group => group.IsEligible(playerGroup)).ToList().RandomObject();
        }

        public sealed class Group
        {
            private readonly List<Entity> entities = new List<Entity>();

            public Group(XmlEntry entry)
            {
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

            public bool IsEligible(PlayerHordeGroup playerGroup)
            {
                return this.entities.Where(entity => entity.IsEligible(playerGroup)).Any();
            }

            public List<Entity> GetEligible(PlayerHordeGroup playerGroup)
            {
                return this.entities.Where(entity => entity.IsEligible(playerGroup)).ToList();
            }

            public sealed class GS
            {
                private readonly int min;
                private readonly int max;

                private readonly List<Entity> entities = new List<Entity>();

                public GS(XmlEntry entry)
                {
                    if(entry.GetAttribute("min", out string minValue))
                        this.min = int.Parse(minValue);

                    if(entry.GetAttribute("max", out string maxValue))
                        this.max = int.Parse(maxValue);

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

            public sealed class Entity
            {
                private readonly GS gs;

                private readonly int entityClass;
                private readonly string entityGroup;

                private readonly int minCount, maxCount;

                public Entity(XmlEntry entry) : this(null, entry) { }

                public Entity(GS gs, XmlEntry entry)
                {
                    this.gs = gs;

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
                    return this.gs == null || this.gs.IsEligible(playerGroup);
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
