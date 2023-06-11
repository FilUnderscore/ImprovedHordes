namespace ImprovedHordes.Core.AI
{
    public interface IAICommandGenerator
    {
        bool GenerateNextCommand(out GeneratedAICommand command);
    }
}
