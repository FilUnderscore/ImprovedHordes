using ImprovedHordes.Core.Abstractions.World.Random;

namespace ImprovedHordes.Core.AI
{
    public interface IAICommandGenerator<CommandType> where CommandType : AICommand
    {
        bool GenerateNextCommand(IWorldRandom worldRandom, out GeneratedAICommand<CommandType> command);
    }
}
