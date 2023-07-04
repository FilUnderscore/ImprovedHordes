using ImprovedHordes.Core.Abstractions.World;
using ImprovedHordes.Core.AI;
using ImprovedHordes.Core.World.Event;
using UnityEngine;

namespace ImprovedHordes.Screamer.Commands
{
    public sealed class ScreamerEntityAICommand : EntityAICommand
    {
        private const float SCREAM_DELAY = 6.0f;
        private const int MAX_SCREAMS = 3;

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

            if ((screamTicks -= dt) <= 0.0 && !entity.IsStunned())
            {
                entity.PlaySound(entity.GetAlertSound());
                this.worldEventReporter.Report(new WorldEvent(entity.GetLocation(), Mathf.Max((screamCount + 1) * 50f, 0f), true));
                screamTicks = SCREAM_DELAY * (screamCount + 1) * (screamCount + 2) * (screamCount + 3);
                screamCount = (screamCount + 1) % MAX_SCREAMS;
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
