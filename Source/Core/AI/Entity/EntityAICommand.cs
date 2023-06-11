using ImprovedHordes.Core.Abstractions;

namespace ImprovedHordes.Core.AI
{
    public abstract class EntityAICommand : AICommand
    {
        public EntityAICommand() 
        {
        }

        public abstract bool CanExecute(IEntity entity);

        public override bool CanExecute(IAIAgent agent)
        {
            if (!(agent is IEntity))
                return false;

            return CanExecute(agent as IEntity);
        }

        public abstract void Execute(IEntity entity, float dt);

        public override void Execute(IAIAgent agent, float dt)
        {
            if(!(agent is IEntity)) return;

            Execute(agent as IEntity, dt);
        }

        public abstract bool IsComplete(IEntity entity);

        public override bool IsComplete(IAIAgent agent)
        {
            if(!(agent is IEntity)) return true;

            return IsComplete(agent as IEntity);
        }

        public abstract int GetObjectiveScore(IEntity entity);

        public override int GetObjectiveScore(IAIAgent agent)
        {
            if (!(agent is IEntity)) return 0;

            return GetObjectiveScore(agent as IEntity);
        }
    }
}
