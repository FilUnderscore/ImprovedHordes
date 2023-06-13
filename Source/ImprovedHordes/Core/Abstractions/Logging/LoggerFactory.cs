using System;

namespace ImprovedHordes.Core.Abstractions.Logging
{
    public interface ILoggerFactory
    {
        ILogger Create(Type type);
    }
}
