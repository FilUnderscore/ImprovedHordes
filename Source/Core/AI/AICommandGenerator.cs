namespace ImprovedHordes.Core.AI
{
    public interface IAICommandGenerator<CommandType> where CommandType : AICommand
    {
        bool GenerateNextCommand(out GeneratedAICommand<CommandType> command);
    }
}
