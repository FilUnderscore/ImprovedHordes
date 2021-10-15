using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde
{
    public class HordesFromXml
    {
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
                        Hordes.hordes[type] = new Dictionary<string, HordeGroup>();
                        
                        foreach(XmlNode childNode in hordeElement.ChildNodes)
                        {
                            if(childNode.NodeType == XmlNodeType.Element && childNode.Name.Equals("hordegroup"))
                            {
                                XmlElement hordegroupElement = (XmlElement)childNode;

                                string hordegroupName = hordegroupElement.HasAttribute("name") ? hordegroupElement.GetAttribute("name") : throw new Exception("[Improved Hordes] Attribute 'name' missing on hordegroup tag.");
                                //HashSet<int> prefWeekDays = hordegroupElement.HasAttribute("prefWeekDay") ? ParsePrefWeekDays(hordegroupElement.GetAttribute("prefWeekDay")) : null;
                                RuntimeEval<HashSet<int>> prefWeekDays = hordegroupElement.HasAttribute("prefWeekDay") ? RuntimeEval<HashSet<int>>.Parse(hordegroupElement.GetAttribute("prefWeekDay"), prefWeekDaysStr => ParsePrefWeekDays(prefWeekDaysStr)) : null;

                                //int? maxWeeklyOccurances = null;
                                RuntimeEval<int> maxWeeklyOccurances = null;

                                if (hordegroupElement.HasAttribute("maxWeeklyOccurances"))
                                    maxWeeklyOccurances = RuntimeEval<int>.Parse(hordegroupElement.GetAttribute("maxWeeklyOccurances"), maxWeeklyOccurancesStr => StringParsers.ParseSInt32(maxWeeklyOccurancesStr));

                                HordeGroup group = new HordeGroup(hordegroupName, prefWeekDays, maxWeeklyOccurances);

                                EvaluateChildNodes(hordegroupElement, group);

                                Hordes.hordes[type].Add(hordegroupName, group);
                            }
                        }

                        if(Hordes.hordes[type].Count == 0)
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

            if (entityName != null && entityGroup != null)
                throw new Exception(String.Format("[Improved Hordes] Horde group {0} has double defined entity with name {1} and group {2}, only one can be defined.", group.name, entityName, entityGroup));

            //int minCount = entityElement.HasAttribute("minCount") ? StringParsers.Parseint32(entityElement.GetAttribute("minCount")) : 0;
            RuntimeEval<int> minCount = entityElement.HasAttribute("minCount") ? RuntimeEval<int>.Parse(entityElement.GetAttribute("minCount"), minCountStr => StringParsers.ParseSInt32(minCountStr)) : null;
            RuntimeEval<float> countIncPerGS = entityElement.HasAttribute("countIncPerGS") ? RuntimeEval<float>.Parse(entityElement.GetAttribute("countIncPerGS"), countIncPerGSStr => StringParsers.ParseFloat(countIncPerGSStr)) : null;
            RuntimeEval<int> maxCount = null;

            if (entityElement.HasAttribute("maxCount"))
                maxCount = RuntimeEval<int>.Parse(entityElement.GetAttribute("maxCount"), maxCountStr => StringParsers.ParseSInt32(maxCountStr));

            HordeGroupEntity entity = new HordeGroupEntity(gs, entityName, entityGroup, minCount, maxCount);

            group.entities.Add(entity);
        }

        private static void EvaluateGSThenEntityNode(XmlElement gsElement, HordeGroup group)
        {
            RuntimeEval<int> minGS = gsElement.HasAttribute("min") ? RuntimeEval<int>.Parse(gsElement.GetAttribute("min"), minValueStr => StringParsers.ParseSInt32(minValueStr)) : null;
            RuntimeEval<int> maxGS = gsElement.HasAttribute("max") ? RuntimeEval<int>.Parse(gsElement.GetAttribute("max"), minValueStr => StringParsers.ParseSInt32(minValueStr)) : null;
            RuntimeEval<int> countDecGS = gsElement.HasAttribute("countDecGS") ? RuntimeEval<int>.Parse(gsElement.GetAttribute("countDecGS"), minValueStr => StringParsers.ParseSInt32(minValueStr)) : null;

            RuntimeEval<float> countIncPerGS = gsElement.HasAttribute("countIncPerGS") ? RuntimeEval<float>.Parse(gsElement.GetAttribute("countIncPerGS"), minValueStr => StringParsers.ParseFloat(minValueStr)) : null;
            RuntimeEval<float> countDecPerPostGS = gsElement.HasAttribute("countDecPerPostGS") ? RuntimeEval<float>.Parse(gsElement.GetAttribute("countDecPerPostGS"), str => StringParsers.ParseFloat(str)) : null;

            GS gs = new GS(minGS, maxGS, countDecGS, countIncPerGS, countDecPerPostGS);

            EvaluateChildNodes(gsElement, group, gs);
        }
    }
}
