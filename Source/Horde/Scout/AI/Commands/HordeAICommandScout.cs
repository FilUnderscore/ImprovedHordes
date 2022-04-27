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

        private readonly ScoutManager manager;
        private readonly Scout scout;

        private readonly List<HordeAICommand> commands;
        private int currentCommandIndex = 0;

        private readonly List<HordeAICommand> scoutCommands = new List<HordeAICommand>();
        private int currentScoutCommandIndex = 0;

        private float attackDelay = 0.0f; // Initial delay of 0 to summon horde instantly when player is spotted. Adds to the difficulty. TODO: Maybe make an option?

        private bool finished = false;

        private bool isScreamer = false;
        private bool isFeral = false;
        private HordeAIEntity.SenseEntry senseEntry;

        public HordeAICommandScout(ScoutManager manager, Scout scout, bool isScreamer)
        {
            this.manager = manager;
            this.scout = scout;

            this.commands = new List<HordeAICommand>(scout.aiEntity.commands);
            this.currentCommandIndex = scout.aiEntity.currentCommandIndex;
        
            this.isScreamer = isScreamer;
            this.isFeral = scout.aiHorde.GetHordeInstance().feral;
        }

        public bool HasCommands()
        {
            return this.currentCommandIndex < this.commands.Count;
        }

        public bool HasOtherCommands()
        {
            return this.currentScoutCommandIndex < this.scoutCommands.Count;
        }

        public HordeAICommand GetCurrentCommand()
        {
            return this.commands[this.currentCommandIndex];
        }

        public HordeAICommand GetCurrentScoutCommand()
        {
            return this.scoutCommands[this.currentScoutCommandIndex];
        }

        public void UpdateTarget(Vector3 target, float value)
        {
#if DEBUG
            Log("[Scout] New target {0}.", target);
#endif

            this.scoutCommands.Clear();
            this.scoutCommands.Add(new HordeAICommandDestination(target, DIST_RADIUS));
            this.scoutCommands.Add(new HordeAICommandWander(value * 3));

            this.currentScoutCommandIndex = 0;
        }

        public override bool CanExecute(EntityAlive alive)
        {
            return !finished;
        }

        public override void Execute(float dt, EntityAlive alive)
        {
            if (alive.GetAttackTarget() == null || !(alive.GetAttackTarget() is EntityPlayer))
            {
                if(HasOtherCommands())
                {
                    if (GetCurrentScoutCommand().CanExecute(alive))
                        GetCurrentScoutCommand().Execute(dt, alive);

                    if (GetCurrentScoutCommand().IsFinished(alive))
                        currentScoutCommandIndex++;
                }
                else if (HasCommands())
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
            else if(isScreamer)
            {
                EntityPlayer target = alive.GetAttackTarget() as EntityPlayer;

                if (attackDelay <= 0.0 && alive.bodyDamage.CurrentStun == EnumEntityStunType.None && !target.IsDead())
                {
                    alive.PlayOneShot(alive.GetSoundAlert());

                    Log("[Scout] Scout trying to summon horde for {0}.", target.EntityName);
                    this.manager.TrySpawnScoutHorde(target, scout); // Spawn horde.

                    this.UpdateTarget(target.position, WANDER_TIME);

                    attackDelay = ATTACK_DELAY / (isFeral ? 2 : 1) * (this.manager.GetCurrentSpawnedScoutHordesCount(target.position) + 1); // Delay screamers longer while more zombies are present.
                }
                else if (attackDelay > 0.0)
                {
                    attackDelay -= dt;
                }
            }
        }

        public override bool CanInterruptWithSense(HordeAIEntity.SenseEntry entry)
        {
            if(this.senseEntry != entry)
                this.senseEntry = entry;

            return false;
        }

        public override bool IsFinished(EntityAlive alive)
        {
            return finished;
        }
    }
}
