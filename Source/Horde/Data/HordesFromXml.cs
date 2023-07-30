using System;
using System.Collections.Generic;
using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Data
{
    public class HordesFromXml
    {
        // TODO: Rewrite XML Element reading, have parsers that allow for deeper nodes and element attribute parsing. E.g. XML base struct that can be inherited and build upon, for hordes, for entities etc.
        public static void LoadHordes(XmlFile xmlFile)
        {
            XmlFileParser parser = new XmlFileParser(xmlFile);

            parser.GetEntries("horde").ForEach(hordeEntry =>
            {
                if (!hordeEntry.GetAttribute("type", out string type))
                    throw new Exception("[Improved Hordes Legacy] Attribute 'type' missing on horde tag.");

                HordeGroupList hordeGroupList = new HordeGroupList(type);
                HordesList.hordes.Add(type, hordeGroupList);

                hordeEntry.GetEntries("hordegroup").ForEach(hordeGroupEntry =>
                {
                    hordeGroupEntry.GetAttribute("horde", out string horde);
                    
                    if(!hordeGroupEntry.GetAttribute("name", out string hordegroupName))
                        throw new Exception("[Improved Hordes Legacy] Attribute 'name' missing on hordegroup tag.");

                    if (horde != null && HordesList.hordes.ContainsKey(horde) && HordesList.hordes[horde].hordes.ContainsKey(hordegroupName)) // To avoid repetition of hordes if needed.
                    {
                        var referencedHorde = HordesList.hordes[horde].hordes[hordegroupName];
                        hordeGroupList.hordes.Add(hordegroupName, referencedHorde);

                        return;
                    }

                    hordeGroupEntry.GetAttribute("parent", out string parent);
                    RuntimeEval.Value<float> weight = ParseIfExists<float>(hordeGroupEntry, "weight");
                    RuntimeEval.Value<HashSet<int>> prefWeekDays = ParseIfExists<HashSet<int>>(hordeGroupEntry, "prefWeekDay", str => ParsePrefWeekDays(str));
                    RuntimeEval.Value<int> maxWeeklyOccurrences = ParseIfExists<int>(hordeGroupEntry, "maxWeeklyOccurrences");

                    HordeGroup group = new HordeGroup(hordeGroupList, parent, hordegroupName, weight, prefWeekDays, maxWeeklyOccurrences);

                    EvaluateChildNodes(hordeGroupEntry, group);

                    hordeGroupList.hordes.Add(hordegroupName, group);
                });

                hordeGroupList.SortParentsAndChildrenOut();

                if (hordeGroupList.hordes.Count == 0)
                {
                    throw new Exception(string.Format("[Improved Hordes Legacy] Empty hordes are not allowed. Horde type: {0}", type));
                }
            });
        }

        private static RuntimeEval.Value<T> ParseIfExists<T>(XmlEntry entry, string attribute, Func<string, T> parser = null)
        {
            RuntimeEval.Value<T> value = null;

            if (entry.GetAttribute(attribute, out string attributeStr))
                value = RuntimeEval.Value<T>.Parse(attributeStr, parser);

            return value;
        }

        private static HashSet<int> ParsePrefWeekDays(string str)
        {
            HashSet<int> weekDays = new HashSet<int>();

            try
            {
                foreach (var substr in str.Split(','))
                {
                   int weekDay = int.Parse(substr);

                    if(!weekDays.Contains(weekDay))
                        weekDays.Add(weekDay);
                }
            }
            catch(Exception )
            {
                Error("[Improved Hordes Legacy] Failed to parse preferred week days: {0} - hordegroup will not spawn.", str);
            }

            return weekDays;
        }

        private static void EvaluateChildNodes(XmlEntry parentEntry, HordeGroup group, GS gs = null)
        {
            parentEntry.GetEntries("entity").ForEach(entityEntry =>
            {
                EvaluateEntityNode(entityEntry, group, gs);
            });

            parentEntry.GetEntries("gs").ForEach(gsEntry =>
            {
                EvaluateGSThenEntityNode(gsEntry, group);
            });
        }

        private static void EvaluateEntityNode(XmlEntry entityEntry, HordeGroup group, GS gs = null)
        {
            entityEntry.GetAttribute("name", out string entityName);
            entityEntry.GetAttribute("group", out string entityGroup);
            entityEntry.GetAttribute("horde", out string horde);

            if (entityName != null && entityGroup != null)
                throw new Exception(String.Format("[Improved Hordes Legacy] Horde group {0} has double defined entity with name {1} and group {2}, only one can be defined.", group.name, entityName, entityGroup));

            RuntimeEval.Value<HashSet<string>> biomes = ParseIfExists<HashSet<string>>(entityEntry, "biomes", str => ParseBiomes(str));
            RuntimeEval.Value<float> chance = ParseIfExists<float>(entityEntry, "chance");

            RuntimeEval.Value<int> minCount = ParseIfExists<int>(entityEntry, "minCount");
            RuntimeEval.Value<int> maxCount = ParseIfExists<int>(entityEntry, "maxCount");

            RuntimeEval.Value<ETimeOfDay> timeOfDay = ParseIfExists<ETimeOfDay>(entityEntry, "time", str => ParseTimeOfDay(str));

            RuntimeEval.Value<POITags> tags = ParseIfExists<POITags>(entityEntry, "tags", str => POITags.Parse(str));

            HordeGroupEntity entity = new HordeGroupEntity(gs, entityName, entityGroup, horde, biomes, chance, minCount, maxCount, timeOfDay, tags);

            group.entities.Add(entity);
        }

        private static ETimeOfDay ParseTimeOfDay(string str)
        {
            if(str.EqualsCaseInsensitive("day"))
            {
                return ETimeOfDay.Day;
            }
            else if(str.EqualsCaseInsensitive("night"))
            {
                return ETimeOfDay.Night;
            }

            return ETimeOfDay.Anytime;
        }

        private static HashSet<string> ParseBiomes(string str)
        {
            HashSet<string> biomes = new HashSet<string>();

            try
            {
                foreach (var substr in str.Split(','))
                {
                    biomes.Add(substr);
                }
            }
            catch (Exception)
            {
                Error("[Improved Hordes Legacy] Failed to parse biome: {0} - hordegroup will not spawn.", str);
            }

            return biomes;
        }

        private static void EvaluateGSThenEntityNode(XmlEntry gsEntry, HordeGroup group)
        {
            RuntimeEval.Value<int> minGS = ParseIfExists<int>(gsEntry, "min");
            RuntimeEval.Value<int> maxGS = ParseIfExists<int>(gsEntry, "max");
            RuntimeEval.Value<int> countDecGS = ParseIfExists<int>(gsEntry, "countDecGS");

            RuntimeEval.Value<float> countIncPerGS = ParseIfExists<float>(gsEntry, "countIncPerGS"); 
            RuntimeEval.Value<float> countDecPerPostGS = ParseIfExists<float>(gsEntry, "countDecPerPostGS");

            GS gs = new GS(minGS, maxGS, countDecGS, countIncPerGS, countDecPerPostGS);

            EvaluateChildNodes(gsEntry, group, gs);
        }
    }
}
