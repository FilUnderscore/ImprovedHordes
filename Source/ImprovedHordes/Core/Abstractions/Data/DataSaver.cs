namespace ImprovedHordes.Core.Abstractions.Data
{
    public interface IDataSaver
    {
        void Save<T>(T data);
    }
}
