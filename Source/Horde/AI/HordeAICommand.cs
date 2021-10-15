namespace ImprovedHordes.Horde.AI
{
    public abstract class HordeAICommand
    {
        public abstract bool CanExecute(EntityAlive alive);

        public abstract bool IsFinished(EntityAlive alive);

        public abstract void Execute(float dt, EntityAlive alive);
    }
}
