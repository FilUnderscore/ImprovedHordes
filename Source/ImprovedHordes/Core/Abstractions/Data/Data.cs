namespace ImprovedHordes.Core.Abstractions.Data
{
    public interface IData
    {
        IData Load(IDataLoader loader);
        void Save(IDataSaver saver);
    }
}
