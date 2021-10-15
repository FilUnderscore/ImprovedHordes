using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImprovedHordes.Horde.AI
{
    public abstract class HordeAICommand
    {
        public abstract bool CanExecute(EntityAlive alive);

        public abstract bool IsFinished(EntityAlive alive);

        public abstract void Execute(double dt, EntityAlive alive);
    }
}
