using ImprovedHordes.Source.Core.AI;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster.AI
{
    public interface IAICommandGenerator
    {
        bool GenerateNextCommand(out GeneratedAICommand command);
    }
}
