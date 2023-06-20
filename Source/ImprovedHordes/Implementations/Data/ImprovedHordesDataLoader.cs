using ImprovedHordes.Core.Abstractions.Data;
using ImprovedHordes.Core.Abstractions.Logging;
using System;
using System.IO;

namespace ImprovedHordes.Implementations.Data
{
    public sealed class ImprovedHordesDataLoader : IDataLoader, IDisposable
    {
        private readonly ILogger logger;
        private readonly IDataParserRegistry dataParserRegistry;
        private readonly BinaryReader reader;

        public ImprovedHordesDataLoader(ILoggerFactory loggerFactory, IDataParserRegistry dataParserRegistry, BinaryReader reader)
        {
            this.logger = loggerFactory.Create(typeof(ImprovedHordesDataLoader));
            this.dataParserRegistry = dataParserRegistry;
            this.reader = reader;
        }

        public void Dispose()
        {
            this.reader.Dispose();
        }

        public T Load<T>()
        {
            if(!typeof(T).IsValueType)
            {
                if (!this.reader.ReadBoolean())
                    return default(T);
            }

            if (typeof(IData).IsAssignableFrom(typeof(T)))
            {
                IData data = (IData)Activator.CreateInstance(typeof(T), true);

                if (data == null)
                {
                    this.logger.Error($"Type {typeof(T).Name} does not have a default constructor. Returning default value.");
                    return default(T);
                }

                return (T)data.Load(this);
            }
            else
            {
                IDataParser<T> dataParser;

                if (this.dataParserRegistry == null || (dataParser = this.dataParserRegistry.GetDataParser<T>()) == null)
                {
                    Type type = this.Load<Type>();

                    // Runtime type parsing.
                    if(this.dataParserRegistry != null)
                    {
                        IRuntimeDataParser runtimeDataParser = this.dataParserRegistry.GetRuntimeDataParser(type);

                        if(runtimeDataParser != null)
                            return (T)runtimeDataParser.Load(this, reader);
                    }

                    return (T)Activator.CreateInstance(type, true);
                }

                return dataParser.Load(this, this.reader);
            }
        }
    }
}
