using System;

namespace ImprovedHordes.Core.AI
{
    public sealed class GeneratedAICommand<CommandType> where CommandType : AICommand
    {
        public readonly CommandType Command;

        public readonly Action<CommandType> OnComplete;
        public readonly Action<CommandType> OnInterrupt;

        public GeneratedAICommand(CommandType command, Action<CommandType> onComplete, Action<CommandType> onInterrupt)
        {
            this.Command = command;
            this.OnComplete = onComplete;
            this.OnInterrupt = onInterrupt;
        }

        public GeneratedAICommand(CommandType command, Action<CommandType> onComplete) : this(command, onComplete, null) { }
        
        public GeneratedAICommand(CommandType command) : this(command, null) { }
    }
}
