using System.Collections.Generic;

namespace ImprovedHordes.Horde.AI
{
    public class HordeAIEntity
    {
        public Horde horde;

        public EntityAlive alive;
        public bool despawnOnCompletion;
        
        public List<HordeAICommand> commands;
        public int currentCommandIndex = 0;

        public HordeAIEntity(EntityAlive alive, bool despawnOnCompletion, List<HordeAICommand> commands)
        {
            this.alive = alive;
            this.despawnOnCompletion = despawnOnCompletion;
            this.commands = commands;
        }

        public HordeAIEntity(Horde horde, EntityAlive alive, bool despawnOnCompletion, List<HordeAICommand> commands) : this(alive, despawnOnCompletion, commands)
        {
            this.horde = horde;
        }

        public void InterruptWithNewCommands(params HordeAICommand[] commands)
        {
            int size = this.commands.Count;
            this.commands.AddRange(commands);

            this.currentCommandIndex = 0;
            this.commands.RemoveRange(0, size);
        }
    }
}
