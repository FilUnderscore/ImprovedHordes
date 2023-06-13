using ImprovedHordes.Core.Abstractions.Logging;
using System;

namespace ImprovedHordes.Implementations.Logging
{
    public sealed class ImprovedHordesLoggerFactory : ILoggerFactory
    {
        private static ImprovedHordesLoggerFactory Instance;

        public ImprovedHordesLoggerFactory()
        {
            Instance = this;
        }

        public ILogger Create(Type type)
        {
            return new ImprovedHordesLogger(type);
        }

        public static bool TryGetInstance(out ImprovedHordesLoggerFactory ihLoggerFactory)
        {
            ihLoggerFactory = Instance;
            return Instance != null;
        }
    }
}
