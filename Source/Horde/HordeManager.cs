using System.Collections.Generic;

using ImprovedHordes.Horde.AI.Events;

namespace ImprovedHordes.Horde
{
    public class HordeManager : IManager
    {
        private readonly List<Horde> hordes = new List<Horde>();

        private readonly ImprovedHordesManager manager;

        public HordeManager(ImprovedHordesManager manager)
        {
            this.manager = manager;
            // TODO: Rework hordes to all be per player, e.g. each player has their own wandering horde schedule that can merge if nearby players have similar occurances.
        }

        public void RegisterHorde(Horde horde)
        {
            hordes.Add(horde);

            this.manager.AIManager.OnHordeKilled += OnHordeKilled;
        }

        private void OnHordeKilled(object sender, HordeKilledEvent e)
        {
            ImprovedHordesManager.Instance.HordeManager.DeregisterHorde(e.horde.GetHordeInstance());
        }

        public void DeregisterHorde(Horde horde)
        {
            hordes.Remove(horde);
        }

        public Dictionary<PlayerHordeGroup, List<Horde>> GetAllHordes()
        {
            Dictionary<PlayerHordeGroup, List<Horde>> allHordes = new Dictionary<PlayerHordeGroup, List<Horde>>();
        
            foreach(var horde in this.hordes)
            {
                if(!allHordes.ContainsKey(horde.playerGroup))
                    allHordes.Add(horde.playerGroup, new List<Horde>());

                allHordes[horde.playerGroup].Add(horde);
            }

            return allHordes;
        }

        public void Shutdown()
        {
            this.hordes.Clear();
        }
    }
}
