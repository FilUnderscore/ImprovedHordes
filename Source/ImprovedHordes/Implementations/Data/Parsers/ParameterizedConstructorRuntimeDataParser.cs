using ImprovedHordes.Core.Abstractions.Data;
using System;
using System.IO;

namespace ImprovedHordes.Implementations.Data.Parsers
{
    public sealed class ParameterizedConstructorRuntimeDataParser<T> : IRuntimeDataParser
    {
        private readonly Func<IDataLoader, T> constructorFunc;
        private readonly Action<T, IDataSaver> saveAction;

        public ParameterizedConstructorRuntimeDataParser(Func<IDataLoader, T> constructorFunc, Action<T, IDataSaver> saveAction = null)
        {
            this.constructorFunc = constructorFunc;
            this.saveAction = saveAction;
        }

        public object Load(IDataLoader loader, BinaryReader reader)
        {
            return this.constructorFunc.Invoke(loader);
        }

        public void Save(IDataSaver saver, BinaryWriter writer, object obj)
        {
            if (this.saveAction == null)
                return;

            this.saveAction.Invoke((T)obj, saver);
        }
    }
}
