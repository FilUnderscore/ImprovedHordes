using System.Collections.Generic;

using ImprovedHordes.Horde.AI;
using ImprovedHordes.Horde.AI.Commands;
using ImprovedHordes.Horde.Wandering.AI.Commands;

using UnityEngine;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Scout.AI.Commands
{
    class HordeAICommandScout : HordeAICommand
    {
        private const int DIST_RADIUS = 10;
        private const float ATTACK_DELAY = 18.0f;
        private const float WANDER_TIME = 90.0f;

        // TODO. Intercept horde entity commands.
        private readonly ScoutManager manager;

        private readonly List<HordeAICommand> commands;
        private int currentCommandIndex = 0;

        private float attackDelay = ATTACK_DELAY;

        private bool finished = false;

        public HordeAICommandScout(ScoutManager manager, HordeAIEntity entity)
        {
            this.manager = manager;

            this.commands = new List<HordeAICommand>(entity.commands);
            this.currentCommandIndex = entity.currentCommandIndex;
        }

        public bool HasOtherCommands()
        {
            return this.currentCommandIndex < this.commands.Count;
        }

        public HordeAICommand GetCurrentCommand()
        {
            return this.commands[this.currentCommandIndex];
        }

        public void UpdateTarget(Vector3 target)
        {
            Log("[Scout] New target {0}.", target);

            List<HordeAICommand> newCommands = new List<HordeAICommand>();
            newCommands.Add(new HordeAICommandDestination(target, DIST_RADIUS));
            newCommands.Add(new HordeAICommandWander(WANDER_TIME));

            commands.InsertRange(currentCommandIndex, newCommands);
        }

        public override bool CanExecute(EntityAlive alive)
        {
            return !finished;
        }

        public override void Execute(float dt, EntityAlive alive)
        {
            if (alive.GetAttackTarget() == null || !(alive.GetAttackTarget() is EntityPlayer))
            {
                if (HasOtherCommands())
                {
                    if (GetCurrentCommand().CanExecute(alive))
                        GetCurrentCommand().Execute(dt, alive);

                    if (GetCurrentCommand().IsFinished(alive))
                        currentCommandIndex++;
                }
                else
                {
                    finished = true;
                }
            }
            else
            {
                EntityPlayer target = alive.GetAttackTarget() as EntityPlayer;

                if (attackDelay <= 0.0 && alive.bodyDamage.CurrentStun == EnumEntityStunType.None && !target.IsDead())
                {
                    alive.PlayOneShot(alive.GetSoundAlert());

                    this.manager.SpawnScoutHorde(target); // Spawn horde.
                    this.UpdateTarget(target.position); // TODO check.

                    attackDelay = ATTACK_DELAY;
                }
                else if (attackDelay > 0.0)
                {
                    attackDelay -= dt;
                }
            }
        }

        public override bool IsFinished(EntityAlive alive)
        {
            return finished;
        }
    }
}
