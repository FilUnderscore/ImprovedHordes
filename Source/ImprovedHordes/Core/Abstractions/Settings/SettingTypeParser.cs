namespace ImprovedHordes.Core.Abstractions.Settings
{
    public interface ISettingTypeParser
    {
    }

    public interface ISettingTypeParser<T> : ISettingTypeParser
    {
        bool Parse(string value, out T parsedValue);
    }
}
