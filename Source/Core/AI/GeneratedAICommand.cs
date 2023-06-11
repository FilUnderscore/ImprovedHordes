using System;

namespace ImprovedHordes.Core.AI
{
    public sealed class GeneratedAICommand
    {
        public readonly AICommand Command;

        public readonly Action<AICommand> OnComplete;
        public readonly Action<AICommand> OnInterrupt;

        public GeneratedAICommand(AICommand command, Action<AICommand> onComplete, Action<AICommand> onInterrupt)
        {
            this.Command = command;
            this.OnComplete = onComplete;
            this.OnInterrupt = onInterrupt;
        }

        public GeneratedAICommand(AICommand command, Action<AICommand> onComplete) : this(command, onComplete, null) { }
        
        public GeneratedAICommand(AICommand command) : this(command, null) { }
    }
}
