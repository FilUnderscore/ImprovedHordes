namespace ImprovedHordes.Core.Abstractions.Settings
{
    public interface ISettingLoader
    {
        void RegisterTypeParser<T>(ISettingTypeParser<T> typeParser);
        bool Load<T>(string path, out T value);
    }
}
