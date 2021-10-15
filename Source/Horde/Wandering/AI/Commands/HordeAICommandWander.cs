using ImprovedHordes.Horde.AI;

namespace ImprovedHordes.Horde.Wandering.AI.Commands
{
    public class HordeAICommandWander : HordeAICommand
    {
        private float time;

        public HordeAICommandWander(float time)
        {
            this.time = time;
        }

        public override void Execute(float dt, EntityAlive alive)
        {
            time -= dt;
            alive.ResetDespawnTime();
        }

        public override bool CanExecute(EntityAlive alive)
        {
            return true;
        }

        public override bool IsFinished(EntityAlive alive)
        {
            return time <= 0.0f && alive.GetAttackTarget() == null;
        }
    }
}
