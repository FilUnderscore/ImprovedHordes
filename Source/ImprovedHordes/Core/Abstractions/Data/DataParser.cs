using System.IO;

namespace ImprovedHordes.Core.Abstractions.Data
{
    public interface IDataParser
    {
    }

    public interface IDataParser<T> : IDataParser
    {
        T Load(IDataLoader loader, BinaryReader reader);
        void Save(IDataSaver saver, BinaryWriter writer, T obj);
    }

    public interface IRuntimeDataParser : IDataParser<object>
    {
    }
}
