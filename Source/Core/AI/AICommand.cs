using UnityEngine;

namespace ImprovedHordes.Core.AI
{
    public abstract class AICommand
    {
        private readonly bool canExpire;
        private readonly double timeAssigned;
        private readonly double expiryTimeSeconds;

        public AICommand(double expiryTimeSeconds)
        {
            this.canExpire = true;
            this.timeAssigned = Time.timeAsDouble;
            this.expiryTimeSeconds = expiryTimeSeconds;
        }

        public AICommand()
        {
            this.canExpire = false;
            this.timeAssigned = 0;
            this.expiryTimeSeconds = 0;
        }

        public abstract bool CanExecute(IAIAgent agent);
        
        public abstract void Execute(IAIAgent agent, float dt);

        public abstract bool IsComplete(IAIAgent agent);

        public abstract int GetObjectiveScore(IAIAgent agent);

        public bool HasExpired()
        {
            return this.canExpire && Time.timeAsDouble - timeAssigned > expiryTimeSeconds;
        }
    }
}