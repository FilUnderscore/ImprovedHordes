using System;
using System.Collections.Generic;
using System.Text;

using ImprovedHordes.Horde.Wandering;

namespace ImprovedHordes
{
    class ImprovedHordesCommand : ConsoleCmdAbstract
    {
        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count >= 1)
            {
                if (_params[0].EqualsCaseInsensitive("wandering"))
                {
                    this.ExecuteWandering(_params);
                }
                else if(_params[0].EqualsCaseInsensitive("list"))
                {
                    this.ExecuteList(_params);
                }
            }
            else
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("No sub command given.");
            }
        }

        private void ExecuteWandering(List<string> _params)
        {
            if (_params.Count >= 2)
            {
                if (_params[1].EqualsCaseInsensitive("spawn"))
                {
                    this.ExecuteWanderingSpawn(_params);
                }
                else if (_params[1].EqualsCaseInsensitive("reset"))
                {
                    this.ExecuteWanderingReset(_params);
                }
                else if (_params[1].EqualsCaseInsensitive("show"))
                {
                    this.ExecuteWanderingShow(_params);
                }
            }
            else
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("No sub command given.");
            }
        }

        private void ExecuteWanderingSpawn(List<string> _params)
        {
            bool feral = false;
            if (_params.Count >= 3)
            {
                if (_params[2].EqualsCaseInsensitive("feral"))
                {
                    feral = true;
                }
            }

            ImprovedHordesManager.Instance.WanderingHorde.ForceSpawnWanderingHorde(feral);

            SingletonMonoBehaviour<SdtdConsole>.Instance.Output("[Improved Hordes] Wandering Hordes spawning for all groups.");
        }

        private void ExecuteWanderingReset(List<string> _params)
        {
            var wanderingHorde = ImprovedHordesManager.Instance.WanderingHorde;

            wanderingHorde.spawner.StopAllSpawning();
            wanderingHorde.DisbandAllWanderingHordes();
            wanderingHorde.schedule.Reset();

            SingletonMonoBehaviour<SdtdConsole>.Instance.Output("[Improved Hordes] Wandering Horde weekly schedule reset.");
        }

        private void ExecuteWanderingShow(List<string> _params)
        {
            var wanderingHorde = ImprovedHordesManager.Instance.WanderingHorde;
            var schedule = wanderingHorde.schedule;

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Settings:");
            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordeSchedule.DAYS_PER_RESET), schedule.DAYS_PER_RESET));
            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordeSchedule.HOURS_TO_FIRST_OCCURRENCE_MIN), schedule.HOURS_TO_FIRST_OCCURRENCE_MIN));
            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordeSchedule.HOURS_IN_WEEK_FOR_LAST_OCCURRENCE_MAX), schedule.HOURS_IN_WEEK_FOR_LAST_OCCURRENCE_MAX));
            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordeSchedule.MIN_HRS_BETWEEN_OCCURRENCES), schedule.MIN_HRS_BETWEEN_OCCURRENCES));
            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordeSchedule.MIN_OCCURRENCES), schedule.MIN_OCCURRENCES));
            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordeSchedule.MAX_OCCURRENCES), schedule.MAX_OCCURRENCES));
            builder.AppendLine("");
            builder.AppendLine("Schedule:");
            for (int i = 0; i < schedule.occurrences.Count; i++)
            {
                var occurrence = schedule.occurrences[i];
                var worldTime = occurrence.worldTime;
                var feral = occurrence.feral;

                var (Days, Hours, Minutes) = GameUtils.WorldTimeToElements(worldTime);
                builder.AppendLine(String.Format("- {0} at Day {1} {2:D2} {3:D2} ({4})", i + 1, Days, Hours, Minutes, feral ? "Feral" : "Not Feral"));
            }

            builder.AppendLine("");

            if (wanderingHorde.state == WanderingHordeManager.EHordeState.Finished)
            {
                if (schedule.currentOccurrence < schedule.occurrences.Count)
                    builder.AppendLine(String.Format("Next Occurrence {0} ", schedule.currentOccurrence + 1));
                else
                    builder.AppendLine("No more occurrences this week.");
            }
            else
            {
                builder.AppendLine(String.Format("Current Occurrence: {0}", schedule.currentOccurrence + 1));
                builder.AppendLine(String.Format("State: {0}", Enum.GetName(typeof(WanderingHordeManager.EHordeState), wanderingHorde.state)));
            }

            builder.AppendLine("");

            var resetWorldTime = schedule.nextResetTime;
            var resetWorldTimeElements = GameUtils.WorldTimeToElements(resetWorldTime);
            builder.AppendLine(String.Format("Next reset at Day {0} {1:D2} {2:D2}", resetWorldTimeElements.Days, resetWorldTimeElements.Hours, resetWorldTimeElements.Minutes));

            SingletonMonoBehaviour<SdtdConsole>.Instance.Output(builder.ToString());
        }

        private void ExecuteList(List<String> _params)
        {
            var allHordes = ImprovedHordesManager.Instance.HordeManager.GetAllHordes();

            StringBuilder builder = new StringBuilder();

            if (allHordes.Count > 0)
            {
                foreach (var entry in allHordes)
                {
                    var playerGroup = entry.Key;
                    var hordes = entry.Value;

                    builder.AppendLine(playerGroup.ToString());

                    foreach (var horde in hordes)
                    {
                        builder.AppendLine(" - " + horde.ToString());
                    }
                }
            }
            else
            {
                builder.AppendLine("No hordes are currently occurring.");
            }

            SingletonMonoBehaviour<SdtdConsole>.Instance.Output(builder.ToString());
        }

        public override string[] GetCommands()
        {
            return new string[] { "improvedhordes", "ih" };        
        }

        public override string GetDescription()
        {
            return "Execute a function from the Improved Hordes Mod. `help improvedhordes` for more information.";
        }

        public override string GetHelp()
        {
            return "Commands: \nimprovedhordes list - Shows all player groups and their associated hordes."
                + "\nimprovedhordes wandering spawn (feral) - Spawns a wandering horde for all groups on the server. Non-Feral unless feral argument is specified (optional)."
                + "\nimprovedhordes wandering reset - Resets the weekly schedule for the wandering hordes."
                + "\nimprovedhordes wandering show - Shows information regarding the weekly wandering horde schedule.";
        }
    }
}
