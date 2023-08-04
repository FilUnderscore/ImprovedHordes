using ImprovedHordes.Core.Abstractions.Logging;

namespace ImprovedHordes.Test.Models
{
    public sealed class TestLoggerFactory : ILoggerFactory
    {
        public ILogger Create(Type type)
        {
            return new TestLogger();
        }

        private class TestLogger : ILogger
        {
            public void Error(string message)
            {
                Console.Error.WriteLine($"ERR {message}");
            }

            public void Exception(Exception e)
            {
                Console.Error.WriteLine($"EXC {e.Message}:\n {e.StackTrace}");
            }

            public void Info(string message)
            {
                Console.WriteLine($"INF {message}");
            }

            public void Verbose(string message)
            {
                Console.WriteLine($"VERBOSE {message}");
            }

            public void Warn(string message)
            {
                Console.WriteLine($"WRN {message}");
            }
        }
    }
}
