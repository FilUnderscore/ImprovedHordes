using ImprovedHordes.Source.Horde.AI;

namespace ImprovedHordes.Source.Core.Horde.World.Cluster.AI
{
    public interface IAICommandGenerator
    {
        bool GenerateNextCommand(out AICommand command);
    }
}
