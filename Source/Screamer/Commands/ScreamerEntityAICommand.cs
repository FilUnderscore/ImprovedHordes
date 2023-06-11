using ImprovedHordes.Core.Abstractions;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Event;

namespace ImprovedHordes.Screamer.Commands
{
    public sealed class ScreamerEntityAICommand : EntityAICommand
    {
        private const float SCREAM_DELAY = 18.0f;

        private readonly WorldEventReporter worldEventReporter;

        private float screamTicks = SCREAM_DELAY;
        private int screamCount = 0;

        public ScreamerEntityAICommand(WorldEventReporter worldEventReporter) 
        {
            this.worldEventReporter = worldEventReporter;
        }

        public override bool CanExecute(IEntity entity)
        {
            return true;
        }

        public override void Execute(IEntity entity, float dt)
        {
            if (entity.GetTarget() == null || !entity.GetTarget().IsPlayer())
                return;

            if (screamTicks <= 0.0 && !entity.IsStunned())
            {
                entity.PlaySound(entity.GetAlertSound());
                this.worldEventReporter.Report(new WorldEvent(entity.GetLocation(), 33f));

                screamTicks = SCREAM_DELAY * (screamCount + 1);
                screamCount = screamCount % 3;
            }
            else
            {
                screamTicks -= dt;
            }
        }

        public override int GetObjectiveScore(IEntity entity)
        {
            return screamCount * 100;
        }

        public override bool IsComplete(IEntity entity)
        {
            return false;
        }
    }
}
