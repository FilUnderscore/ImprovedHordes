using ImprovedHordes.Core.Abstractions.Settings;

namespace ImprovedHordes.Implementations.Settings.Parsers
{
    public sealed class ImprovedHordesSettingTypeParserFloat : ISettingTypeParser<float>
    {
        public bool Parse(string value, out float parsedValue)
        {
            return float.TryParse(value, out parsedValue);
        }
    }
}
