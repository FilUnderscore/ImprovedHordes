using System;
using System.Collections.Generic;
using System.IO;

using HarmonyLib;

using ImprovedHordes.Horde.AI.Events;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Wandering
{
    public class WanderingHordeManager
    {
        public readonly HordeManager manager;
        public readonly WanderingHordeSpawner spawner;
        public readonly WanderingHordeSchedule schedule;

        public EHordeState state = EHordeState.Finished;
        public readonly List<Horde> hordes = new List<Horde>();

        public WanderingHordeManager(HordeManager manager)
        {
            this.manager = manager;
            this.schedule = new WanderingHordeSchedule(this);
            this.spawner = new WanderingHordeSpawner(this);

            manager.AIManager.OnHordeKilled += OnWanderingHordeKilled;
        }

        public void Load(BinaryReader reader)
        {
            this.schedule.Load(reader);
        }

        public void Save(BinaryWriter writer)
        {
            this.schedule.Save(writer);
        }

        public void Update()
        {
            if (this.schedule.CheckIfNeedsReset() || this.schedule.IsNextOccuranceDue())
            {
                if(this.schedule.CheckIfNeedsReset())
                    this.schedule.Reset();

                this.spawner.StopAllSpawning();
                this.DisbandAllWanderingHordes();
            }

            if (this.manager.Players.Count == 0)
                return;

            this.spawner.Update();

            if (ShouldSpawnWanderingHorde())
            {
                this.spawner.SpawnWanderingHordes();
            }
        }

        private void DisbandAllWanderingHordes()
        {
            Log("[Wandering] Disbanding all hordes.");

            foreach(var horde in this.hordes)
            {
                var aiHorde = this.manager.AIManager.GetAIHorde(horde);
                
                if(aiHorde != null)
                    aiHorde.Disband();
            }

            this.hordes.Clear();
        }

        public void OnWanderingHordeKilled(object sender, HordeKilledEvent e)
        {
            Horde horde = e.horde.GetHordeInstance();

            if (this.state == WanderingHordeManager.EHordeState.StillAlive)
            {
                if (this.hordes.Contains(horde) && !this.spawner.IsStillSpawningFor(horde.playerGroup))
                {
                    int index = this.hordes.IndexOf(horde);
                    Log("[Wandering Horde] Horde {0} has ended, all Zombies have either reached their destination or have been killed.", index + 1);

                    this.hordes.Remove(horde);
                }

                if (this.hordes.Count == 0)
                {
                    Log("[Wandering Horde] Hordes for all groups have ended.");

                    this.schedule.currentOccurance++;
                    this.state = WanderingHordeManager.EHordeState.Finished;
                }
            }
        }

        // Spawn on weekly basis # of hordes
        // Spaced apart a set number of days.
        public bool ShouldSpawnWanderingHorde()
        {
            return this.state == WanderingHordeManager.EHordeState.Finished && this.schedule.IsOccuranceDue();
        }

        public void ForceSpawnWanderingHorde()
        {
            this.schedule.occurances.Insert(this.state == EHordeState.Finished ? this.schedule.currentOccurance : this.schedule.currentOccurance + 1, new WanderingHordeSchedule.Occurance(this.schedule.GetWorldTime(), true));
        }

        public enum EHordeState
        {
            StillAlive,
            Finished
        }

        class HarmonyPatches
        {
            [HarmonyPatch(typeof(AIDirectorWanderingHordeComponent))]
            [HarmonyPatch("Tick")]
            class WanderingHordeSpawnHook
            {
                static bool Prefix()
                {
                    // Prevent default wandering hordes from spawning at all.
                    return false;
                }
            }
        }
    }
}
