using System;
using System.Collections.Generic;
using System.IO;

using HarmonyLib;

using ImprovedHordes.Horde.AI.Events;

using static ImprovedHordes.Utils.Logger;

using CustomModManager.API;

namespace ImprovedHordes.Horde.Wandering
{
    public class WanderingHordeManager : IManager
    {
        private bool s_enabled = true;
        private int s_horde_player_group_dist = 400;

        public int HORDE_PLAYER_GROUP_DISTANCE
        {
            get
            {
                return s_horde_player_group_dist;
            }
        }

        public bool ENABLED
        {
            get
            {
                return s_enabled;
            }
        }
        public readonly ImprovedHordesManager manager;
        public readonly WanderingHordeSpawner spawner;
        public readonly WanderingHordeSchedule schedule;

        public EHordeState state = EHordeState.Finished;
        public readonly List<Horde> hordes = new List<Horde>();

        public WanderingHordeManager(ImprovedHordesManager manager)
        {
            this.manager = manager;
            this.schedule = new WanderingHordeSchedule(this);
            this.spawner = new WanderingHordeSpawner(this);

            manager.AIManager.OnHordeKilled += OnWanderingHordeKilled;
        }

        public void ReadSettings(Settings settings)
        {
            this.s_enabled = settings.GetBool("enabled", true);
            this.s_horde_player_group_dist = settings.GetInt("horde_player_group_dist", 0, false, 400);

            this.schedule.ReadSettings(settings.GetSettings("schedule"));
        }

        public void HookSettings(ModManagerAPI.ModSettings modSettings)
        {
            modSettings.CreateTab("hordeWanderingSettingsTab", "IHxuiHordeWanderingSettingsTab");

            modSettings.Hook<bool>("hordeWanderingEnabled", "IHxuiHordeEnabledModSetting", value => this.s_enabled = value, () => this.s_enabled, toStr => (toStr.ToString(), toStr.ToString()), str =>
            {
                bool success = bool.TryParse(str, out bool val);
                return (val, success);
            }).SetTab("hordeWanderingSettingsTab").SetAllowedValues(new bool[] { true, false });

            modSettings.Hook<int>("hordeWanderingHordePlayerGroupDist", "IHxuiHordeWanderingHordePlayerGroupDistModSetting", value => this.s_horde_player_group_dist = value, () => this.s_horde_player_group_dist, toStr => (toStr.ToString(), toStr.ToString() + "m"), str =>
            {
                bool success = int.TryParse(str, out int val) && val > 0;
                return (val, success);
            }).SetTab("hordeWanderingSettingsTab");

            this.schedule.HookSettings(modSettings);
        }

        public void Load(BinaryReader reader)
        {
            this.schedule.Load(reader);
        }

        public void Save(BinaryWriter writer)
        {
            this.schedule.Save(writer);
        }

        private const ulong SPAWN_ATTEMPT_COOLDOWN = 100UL;
        private ulong lastSpawnAttempt = 0UL;

        public void Update()
        {
            if (this.schedule.CheckIfNeedsReset() || (this.state == EHordeState.StillAlive && this.schedule.IsNextOccurrenceDue()))
            {
                this.spawner.StopAllSpawning();
                this.DisbandAllWanderingHordes();

                if (this.schedule.CheckIfNeedsReset())
                    this.schedule.Reset();
            }

            if (!this.manager.PlayerManager.AnyPlayers())
                return;

            this.spawner.Update();

            if (ShouldSpawnWanderingHorde() && this.state == EHordeState.Finished)
            {
                if (this.manager.World.GetWorldTime() > lastSpawnAttempt + SPAWN_ATTEMPT_COOLDOWN) // Retry spawning if it fails.
                {
                    if (!this.spawner.SpawnWanderingHordes())
                    {
                        lastSpawnAttempt = this.manager.World.GetWorldTime();
                    }
                }
            }
        }

        public void DisbandAllWanderingHordes()
        {
            if (this.hordes.Count == 0)
                return;

            Log("[Wandering Horde] Disbanding all hordes.");

            foreach (var horde in this.hordes)
            {
                var aiHorde = this.manager.AIManager.GetAsAIHorde(horde);

                if (aiHorde != null)
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

                    this.schedule.currentOccurrence++;
                    this.state = WanderingHordeManager.EHordeState.Finished;
                }
            }
        }

        // Spawn on weekly basis # of hordes
        // Spaced apart a set number of days.
        public bool ShouldSpawnWanderingHorde()
        {
            return this.state == WanderingHordeManager.EHordeState.Finished && this.schedule.IsOccurrenceDue();
        }

        public void ForceSpawnWanderingHorde(bool feral)
        {
            this.schedule.occurrences.Insert(this.state == EHordeState.Finished ? this.schedule.currentOccurrence : this.schedule.currentOccurrence + 1, new WanderingHordeSchedule.Occurrence(this.schedule.GetWorldTime(), feral));
        }

        public void Shutdown()
        {
            this.state = EHordeState.Finished;

            this.schedule.Shutdown();
            this.hordes.Clear();
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
                    return !ImprovedHordesManager.Instance.WanderingHorde.ENABLED || !ImprovedHordesMod.IsHost();
                }
            }
        }
    }
}
