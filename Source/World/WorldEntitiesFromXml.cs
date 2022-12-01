using ImprovedHordes.Horde.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.World
{
    public class WorldEntitiesFromXml
    {
        public static void LoadWorldEntities(XmlFile xmlFile)
        {
            XmlElement documentElement = xmlFile.XmlDoc.DocumentElement;

            if (documentElement.ChildNodes.Count == 0)
                throw new Exception("[Improved Hordes] No element <worldentities> found.");

            IEnumerator enumerator = documentElement.ChildNodes.GetEnumerator();

            try
            {
                while(enumerator.MoveNext())
                {
                    XmlNode current = (XmlNode)enumerator.Current;

                    if(current.NodeType == XmlNodeType.Element && current.Name.Equals("worldentity"))
                    {
                        XmlElement worldEntityElement = (XmlElement)current;

                        string type = worldEntityElement.HasAttribute("type") ? worldEntityElement.GetAttribute("type") : throw new Exception("[Improved Hordes] Attribute 'type' missing on worldentity tag.");
                        string name = worldEntityElement.HasAttribute("name") ? worldEntityElement.GetAttribute("name") : throw new Exception("[Improved Hordes] Attribute 'name' missing on worldentity tag.");

                        WorldEntityType worldEntityType = (WorldEntityType)Enum.Parse(typeof(WorldEntityType), type, true);

                        if (!WorldEntitiesList.WorldEntities.ContainsKey(worldEntityType))
                            WorldEntitiesList.WorldEntities.Add(worldEntityType, new List<WorldEntityDefinition>());
                        
                        RuntimeEval.Value<float> chance = ParseIfExists<float>(worldEntityElement, "chance");
                        WorldEntityDefinition definition = new WorldEntityDefinition(name, chance);
                        
                        EvaluateChildNodes(worldEntityElement, definition);
                        
                        WorldEntitiesList.WorldEntities[worldEntityType].Add(definition);
                    }
                }
            }
            finally
            {
                if (enumerator is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private static void EvaluateChildNodes(XmlElement parentElement, WorldEntityDefinition definition, GS gs = null)
        {
            foreach(XmlNode childEntityNode in parentElement.ChildNodes)
            {
                if(childEntityNode.NodeType == XmlNodeType.Element)
                {
                    XmlElement element = (XmlElement)childEntityNode;

                    if(childEntityNode.Name.Equals("entity"))
                    {
                        EvaluateEntityNode(element, definition, gs);
                    }
                    else if(childEntityNode.Name.Equals("gs"))
                    {
                        EvaluateGSThenEntityNode(element, definition);
                    }
                }
            }
        }

        private static RuntimeEval.Value<T> ParseIfExists<T>(XmlElement element, string attribute, Func<string, T> parser = null)
        {
            RuntimeEval.Value<T> value = null;

            if (element.HasAttribute(attribute))
                value = RuntimeEval.Value<T>.Parse(element.GetAttribute(attribute), parser);

            return value;
        }

        private static void EvaluateEntityNode(XmlElement entityElement, WorldEntityDefinition definition, GS gs = null)
        {
            string entityName = entityElement.HasAttribute("name") ? entityElement.GetAttribute("name") : null;
            string entityGroup = entityElement.HasAttribute("group") ? entityElement.GetAttribute("group") : null;

            if (entityName != null && entityGroup != null)
                throw new Exception(string.Format("[Improved Hordes] World Entity Definition has double defined entity with name {1} and group {2}, only one can be defined.", definition.Name, entityName, entityGroup));

            RuntimeEval.Value<HashSet<string>> biomes = ParseIfExists<HashSet<string>>(entityElement, "biomes", str => ParseBiomes(str));
            RuntimeEval.Value<float> chance = ParseIfExists<float>(entityElement, "chance");

            RuntimeEval.Value<ETimeOfDay> timeOfDay = ParseIfExists<ETimeOfDay>(entityElement, "time", str => ParseTimeOfDay(str));

            RuntimeEval.Value<POITags> tags = ParseIfExists<POITags>(entityElement, "tags", str => POITags.Parse(str));

            WorldEntityDefinition.Entity entity = new WorldEntityDefinition.Entity(gs, entityName, entityGroup, biomes, chance, timeOfDay, tags);

            definition.entities.Add(entity);
        }

        private static ETimeOfDay ParseTimeOfDay(string str)
        {
            if (str.EqualsCaseInsensitive("day"))
            {
                return ETimeOfDay.Day;
            }
            else if (str.EqualsCaseInsensitive("night"))
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

        private static void EvaluateGSThenEntityNode(XmlElement gsElement, WorldEntityDefinition definition)
        {
            RuntimeEval.Value<int> minGS = ParseIfExists<int>(gsElement, "min");
            RuntimeEval.Value<int> maxGS = ParseIfExists<int>(gsElement, "max");
            
            GS gs = new GS(minGS, maxGS, null, null, null);

            EvaluateChildNodes(gsElement, definition, gs);
        }
    }
}
