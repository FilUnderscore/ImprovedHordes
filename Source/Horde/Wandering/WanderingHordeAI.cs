using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.Horde.Wandering
{
    sealed class WanderingHordeAI
    {
        private WanderingHordeManager manager;

        private WanderingHorde horde;
        private Dictionary<EntityAlive, Command> trackedEntities = new Dictionary<EntityAlive, Command>();

        public WanderingHordeAI(WanderingHordeManager manager, WanderingHorde horde)
        {
            this.manager = manager;
            this.horde = horde;
        }

        public void Add(EntityAlive entity)
        {

        }

        public void Update(double dt)
        {
            foreach(var entry in trackedEntities)
            {
                EntityAlive entity = entry.Key;
                Command command = entry.Value;


            }
        }
    }

    abstract class Command
    {
        public abstract void Execute(EntityAlive alive);
    }

    abstract class WanderCommand
    {
        
    }

    class WanderToPlayerCommand : WanderCommand
    {

    }

    class WanderToDestinationCommand : WanderCommand
    {

    }
}
