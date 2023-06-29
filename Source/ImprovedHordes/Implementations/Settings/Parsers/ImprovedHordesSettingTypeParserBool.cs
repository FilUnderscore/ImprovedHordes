using ImprovedHordes.Core.Abstractions.Settings;

namespace ImprovedHordes.Implementations.Settings.Parsers
{
    public sealed class ImprovedHordesSettingTypeParserBool : ISettingTypeParser<bool>
    {
        public bool Parse(string value, out bool parsedValue)
        {
            return bool.TryParse(value, out parsedValue);
        }
    }
}
