using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Data
{
    public class HordesFromXml
    {
        // TODO: Rewrite XML Element reading, have parsers that allow for deeper nodes and element attribute parsing. E.g. XML base struct that can be inherited and build upon, for hordes, for entities etc.
        public static void LoadHordes(XmlFile xmlFile)
        {
            XmlElement documentElement = xmlFile.XmlDoc.DocumentElement;

            if (documentElement.ChildNodes.Count == 0)
                throw new Exception("[Improved Hordes] No element <hordes> found.");
            
            IEnumerator enumerator = documentElement.ChildNodes.GetEnumerator();

            try
            {
                while(enumerator.MoveNext())
                {
                    XmlNode current = (XmlNode)enumerator.Current;

                    if(current.NodeType == XmlNodeType.Element && current.Name.Equals("horde"))
                    {
                        XmlElement hordeElement = (XmlElement)current;

                        string type = hordeElement.HasAttribute("type") ? hordeElement.GetAttribute("type") : throw new Exception("[Improved Hordes] Attribute 'type' missing on horde tag.");
                        HordeGroupList hordeGroupList = new HordeGroupList(type);
                        HordesList.hordes.Add(type, hordeGroupList);
                        
                        foreach(XmlNode childNode in hordeElement.ChildNodes)
                        {
                            if(childNode.NodeType == XmlNodeType.Element && childNode.Name.Equals("hordegroup"))
                            {
                                XmlElement hordegroupElement = (XmlElement)childNode;

                                string horde = hordegroupElement.HasAttribute("horde") ? hordegroupElement.GetAttribute("horde") : null;
                                string hordegroupName = hordegroupElement.HasAttribute("name") ? hordegroupElement.GetAttribute("name") : throw new Exception("[Improved Hordes] Attribute 'name' missing on hordegroup tag.");

                                if (horde != null && HordesList.hordes.ContainsKey(horde) && HordesList.hordes[horde].hordes.ContainsKey(hordegroupName)) // To avoid repetition of hordes if needed.
                                {
                                    var referencedHorde = HordesList.hordes[horde].hordes[hordegroupName];
                                    hordeGroupList.hordes.Add(hordegroupName, referencedHorde);

                                    continue;
                                }

                                string parent = hordegroupElement.HasAttribute("parent") ? hordegroupElement.GetAttribute("parent") : null;
                                RuntimeEval.Value<float> weight = ParseIfExists<float>(hordegroupElement, "weight");
                                RuntimeEval.Value<HashSet<int>> prefWeekDays = ParseIfExists<HashSet<int>>(hordegroupElement, "prefWeekDay", str => ParsePrefWeekDays(str));
                                RuntimeEval.Value<int> maxWeeklyOccurrences = ParseIfExists<int>(hordegroupElement, "maxWeeklyOccurrences");

                                HordeGroup group = new HordeGroup(hordeGroupList, parent, hordegroupName, weight, prefWeekDays, maxWeeklyOccurrences);

                                EvaluateChildNodes(hordegroupElement, group);

                                hordeGroupList.hordes.Add(hordegroupName, group);
                            }
                        }

                        hordeGroupList.SortParentsAndChildrenOut();

                        if(hordeGroupList.hordes.Count == 0)
                        {
                            throw new Exception(String.Format("[Improved Hordes] Empty hordes are not allowed. Horde type: {0}", type));
                        }
                    }
                }
            }
            finally
            {
                if (enumerator is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private static RuntimeEval.Value<T> ParseIfExists<T>(XmlElement element, string attribute, Func<string, T> parser = null)
        {
            RuntimeEval.Value<T> value = null;

            if (element.HasAttribute(attribute))
                value = RuntimeEval.Value<T>.Parse(element.GetAttribute(attribute), parser);

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
                Error("[Improved Hordes] Failed to parse preferred week days: {0} - hordegroup will not spawn.", str);
            }

            return weekDays;
        }

        private static void EvaluateChildNodes(XmlElement parentElement, HordeGroup group, GS gs = null)
        {
            foreach (XmlNode childEntityNode in parentElement.ChildNodes)
            {
                if (childEntityNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement element = (XmlElement)childEntityNode;

                    if (childEntityNode.Name.Equals("entity"))
                    {
                        EvaluateEntityNode(element, group, gs);
                    }
                    else if (childEntityNode.Name.Equals("gs"))
                    {
                        EvaluateGSThenEntityNode(element, group);
                    }
                }
            }
        }

        private static void EvaluateEntityNode(XmlElement entityElement, HordeGroup group, GS gs = null)
        {
            string entityName = entityElement.HasAttribute("name") ? entityElement.GetAttribute("name") : null;
            string entityGroup = entityElement.HasAttribute("group") ? entityElement.GetAttribute("group") : null;
            string horde = entityElement.HasAttribute("horde") ? entityElement.GetAttribute("horde") : null;

            if (entityName != null && entityGroup != null)
                throw new Exception(String.Format("[Improved Hordes] Horde group {0} has double defined entity with name {1} and group {2}, only one can be defined.", group.name, entityName, entityGroup));

            RuntimeEval.Value<HashSet<string>> biomes = ParseIfExists<HashSet<string>>(entityElement, "biomes", str => ParseBiomes(str));
            RuntimeEval.Value<float> chance = ParseIfExists<float>(entityElement, "chance");

            RuntimeEval.Value<int> minCount = ParseIfExists<int>(entityElement, "minCount");
            RuntimeEval.Value<int> maxCount = ParseIfExists<int>(entityElement, "maxCount");

            RuntimeEval.Value<ETimeOfDay> timeOfDay = ParseIfExists<ETimeOfDay>(entityElement, "time", str => ParseTimeOfDay(str));

            HordeGroupEntity entity = new HordeGroupEntity(gs, entityName, entityGroup, horde, biomes, chance, minCount, maxCount, timeOfDay);

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
                Error("[Improved Hordes] Failed to parse biome: {0} - hordegroup will not spawn.", str);
            }

            return biomes;
        }

        private static void EvaluateGSThenEntityNode(XmlElement gsElement, HordeGroup group)
        {
            RuntimeEval.Value<int> minGS = ParseIfExists<int>(gsElement, "min");
            RuntimeEval.Value<int> maxGS = ParseIfExists<int>(gsElement, "max");
            RuntimeEval.Value<int> countDecGS = ParseIfExists<int>(gsElement, "countDecGS");

            RuntimeEval.Value<float> countIncPerGS = ParseIfExists<float>(gsElement, "countIncPerGS"); 
            RuntimeEval.Value<float> countDecPerPostGS = ParseIfExists<float>(gsElement, "countDecPerPostGS");

            GS gs = new GS(minGS, maxGS, countDecGS, countIncPerGS, countDecPerPostGS);

            EvaluateChildNodes(gsElement, group, gs);
        }
    }
}
