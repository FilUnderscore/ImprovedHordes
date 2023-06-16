using ImprovedHordes.Core.Abstractions.Logging;
using ImprovedHordes.Core.Abstractions.Settings;
using ImprovedHordes.Data.XML;
using System;
using System.Collections.Generic;

namespace ImprovedHordes.Implementations.Settings
{
    public sealed class ImprovedHordesSettingLoader : ISettingLoader
    {
        private readonly ILogger logger;
        private readonly XmlFileParser parser;
        private readonly Dictionary<Type, ISettingTypeParser> settingTypeParsers = new Dictionary<Type, ISettingTypeParser>();

        public ImprovedHordesSettingLoader(ILoggerFactory loggerFactory, XmlFile settingXmlFile)
        {
            this.logger = loggerFactory.Create(typeof(ImprovedHordesSettingLoader));
            this.parser = new XmlFileParser(settingXmlFile);
        }

        public void LoadSettings()
        {
            Setting.LoadAll(this);
        }

        public void RegisterTypeParser<T>(ISettingTypeParser<T> typeParser)
        {
            settingTypeParsers.Add(typeof(T), typeParser);
        }

        public bool Load<T>(string path, out T value)
        {
            if(!settingTypeParsers.TryGetValue(typeof(T), out var settingTypeParser) || !(settingTypeParser is ISettingTypeParser<T> genericSettingTypeParser)) 
            {
                this.logger.Warn($"No {nameof(ISettingTypeParser)} for type {typeof(T).Name}. Using default value.");

                value = default(T);
                return false;
            }

            string[] subpaths = path.Split('/');
            int subpathIndex = 0;

            XmlEntry entry = parser;
            while (subpathIndex < subpaths.Length)
            {
                entry = entry.GetEntries(subpaths[subpathIndex++])[0];
            }

            if(genericSettingTypeParser.Parse(entry.GetValueAsString(), out T parsedValue))
            {
                value = parsedValue;
                return true;
            }
            else
            {
                this.logger.Warn($"Failed to parse {typeof(T).Name} setting at {path}. Using default value.");

                value = default(T);
                return false;
            }
        }
    }
}
