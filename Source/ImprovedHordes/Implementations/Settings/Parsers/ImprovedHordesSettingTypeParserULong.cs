using ImprovedHordes.Core.Abstractions.Settings;

namespace ImprovedHordes.Implementations.Settings.Parsers
{
    public sealed class ImprovedHordesSettingTypeParserULong : ISettingTypeParser<ulong>
    {
        public bool Parse(string value, out ulong parsedValue)
        {
            return ulong.TryParse(value, out parsedValue);
        }
    }
}
