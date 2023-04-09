namespace ImprovedHordes.Source.Horde.AI
{
    public interface IAICommand
    {
        bool CanExecute(IAIAgent agent);
        
        void Execute(IAIAgent agent, float dt);

        bool IsComplete(IAIAgent agent);
    }
}