namespace ImprovedHordes.Core.Abstractions.Logging
{
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Verbose(string message);
    }
}
