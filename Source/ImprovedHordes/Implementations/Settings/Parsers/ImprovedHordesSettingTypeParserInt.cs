using ImprovedHordes.Core.Abstractions.Settings;

namespace ImprovedHordes.Implementations.Settings.Parsers
{
    public sealed class ImprovedHordesSettingTypeParserInt : ISettingTypeParser<int>
    {
        public bool Parse(string value, out int parsedValue)
        {
            return int.TryParse(value, out parsedValue);
        }
    }
}
