using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Horde.AI.Commands
{
    public sealed class HordeAICommandInvestigate : HordeAICommand
    {
        const float DISTANCE_TOLERANCE = 10.0f;

        private HordeAIEntity.SenseEntry entry;
        private float ticks;

        public HordeAICommandInvestigate(HordeAIEntity.SenseEntry entry)
        {
            this.UpdateEntry(entry);
        }

        public void UpdateEntry(HordeAIEntity.SenseEntry entry)
        {
            this.entry = entry;
            this.ticks = entry.GetValue();
        }

        public HordeAIEntity.SenseEntry GetEntry()
        {
            return this.entry;
        }

        public override bool CanExecute(EntityAlive alive)
        {
            return alive.GetAttackTarget() == null || !(alive.GetAttackTarget() is EntityPlayer);
        }

        public override void Execute(float dt, EntityAlive alive)
        {
            alive.ResetDespawnTime();
            ticks -= dt;

            if ((alive.position - entry.position).sqrMagnitude > (DISTANCE_TOLERANCE * DISTANCE_TOLERANCE))
            {
                if (!alive.HasInvestigatePosition || alive.InvestigatePosition != entry.position)
                    alive.SetInvestigatePosition(entry.position, 6000, false);
            }
            else
            {
                alive.ClearInvestigatePosition();
            }
        }

        public override bool IsFinished(EntityAlive alive)
        {
            return ticks <= 0.0f;
        }
    }
}
