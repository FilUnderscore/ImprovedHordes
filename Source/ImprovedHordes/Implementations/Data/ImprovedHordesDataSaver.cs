using ImprovedHordes.Core.Abstractions.Data;
using System;
using System.IO;

namespace ImprovedHordes.Implementations.Data
{
    public sealed class ImprovedHordesDataSaver : IDataSaver, IDisposable
    {
        private readonly IDataParserRegistry dataParserRegistry;
        private readonly BinaryWriter writer;

        public ImprovedHordesDataSaver(IDataParserRegistry dataParserRegistry, BinaryWriter writer) 
        {
            this.dataParserRegistry = dataParserRegistry;
            this.writer = writer;
        }

        public void Dispose()
        {
            this.writer.Dispose();
        }

        public void Save<T>(T data)
        {
            if (!typeof(T).IsValueType) // Reference types can be null.
            {
                this.writer.Write(data != null);

                if (data == null)
                    return;
            }

            if (data is IData idata)
            {
                idata.Save(this);
            }
            else
            {
                IDataParser<T> dataParser;

                if (this.dataParserRegistry == null || (dataParser = this.dataParserRegistry.GetDataParser<T>()) == null)
                {
                    this.Save<Type>(data.GetType());
                    
                    // Runtime type parsing.
                    if(this.dataParserRegistry != null)
                    {
                        IRuntimeDataParser runtimeDataParser = this.dataParserRegistry.GetRuntimeDataParser(data.GetType());

                        if(runtimeDataParser != null)
                            runtimeDataParser.Save(this, writer, data);
                    }
                    
                    return;
                }

                dataParser.Save(this, this.writer, data);
            }
        }
    }
}
