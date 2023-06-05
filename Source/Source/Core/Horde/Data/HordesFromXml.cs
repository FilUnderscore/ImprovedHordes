using ImprovedHordes.Source.Core.Horde.Data.XML;
using System;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Horde.Data
{
    public sealed class HordesFromXml
    {
        private static readonly Dictionary<HordeDefinition, XmlEntry> cached = new Dictionary<HordeDefinition, XmlEntry>();
        private static readonly Dictionary<string, HordeDefinition> definitions = new Dictionary<string, HordeDefinition>();

        public static void LoadHordes(XmlFile file)
        {
            XmlFileParser parser = new XmlFileParser(file);

            parser.GetEntries("horde").ForEach(entry => ParseHorde(entry));

            foreach(var definition in definitions.Values)
            {
                XmlEntry entry = cached[definition];

                if (entry.GetEntries("merge").Count == 0)
                    continue;

                entry.GetEntries("merge")[0].GetEntries("horde").ForEach(mergeEntry =>
                {
                    if(mergeEntry.GetAttribute("type", out string type))
                    {
                        definition.merge.Add(definitions[type]);
                    }
                });
            }
        }

        private static void ParseHorde(XmlEntry entry)
        {
            if (!entry.GetAttribute("type", out string type))
                throw new Exception("[Improved Hordes] Attribute 'type' missing on horde tag.");

            HordeDefinition hordeDefinition = new HordeDefinition(type, entry);

            definitions.Add(type, hordeDefinition);
            cached.Add(hordeDefinition, entry);
        }

        public static HordeDefinition GetHordeDefinition(string type)
        {
            return definitions[type];
        }
    }
}
