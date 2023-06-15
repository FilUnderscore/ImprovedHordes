using System;

namespace ImprovedHordes.Core.Abstractions.Logging
{
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Exception(Exception e);
        void Verbose(string message);
    }
}
