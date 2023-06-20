using System;

namespace ImprovedHordes.Core.Abstractions.Data
{
    public interface IDataParserRegistry
    {
        void RegisterDataParser<T>(IDataParser dataParser);
        IDataParser<T> GetDataParser<T>();
        IRuntimeDataParser GetRuntimeDataParser(Type type);
    }
}
