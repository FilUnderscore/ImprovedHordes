using ImprovedHordes.Source.Core.Horde.Data.XML;
using System;
using System.Collections.Generic;

namespace ImprovedHordes.Source.Core.Horde.Data
{
    public sealed class HordesFromXml
    {
        private static readonly Dictionary<string, HordeDefinition> definitions = new Dictionary<string, HordeDefinition>();

        public static void LoadHordes(XmlFile file)
        {
            XmlFileParser parser = new XmlFileParser(file);

            parser.GetEntries("horde").ForEach(entry => ParseHorde(entry));
        }

        private static void ParseHorde(XmlEntry entry)
        {
            if (!entry.GetAttribute("type", out string type))
                throw new Exception("[Improved Hordes] Attribute 'type' missing on horde tag.");

            definitions.Add(type, new HordeDefinition(entry));
        }

        public static HordeDefinition GetHordeDefinition(string type)
        {
            return definitions[type];
        }
    }
}
