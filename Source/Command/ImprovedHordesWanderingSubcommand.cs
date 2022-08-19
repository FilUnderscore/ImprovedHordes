using ImprovedHordes.Horde.Wandering;
using System;
using System.Collections.Generic;
using System.Text;

namespace ImprovedHordes.Command
{
    internal class ImprovedHordesWanderingSubcommand : SubcommandBase
    {
        public ImprovedHordesWanderingSubcommand() : 
            base("wandering")
        {
            RegisterSubcommand(new SpawnExecutableSubcommand());
            RegisterSubcommand(new ResetExecutableCommand());
            RegisterSubcommand(new ShowExecutableCommand());
        }

        public override string GetDescription()
        {
            return "Wandering Horde command category.";
        }

        class SpawnExecutableSubcommand : ExecutableSubcommandBase
        {
            public SpawnExecutableSubcommand() : base("spawn")
            {

            }

            public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
            {
                bool feral = false;

                if (args.Count > 0 && args[0].EqualsCaseInsensitive("feral"))
                    feral = true;

                ImprovedHordesManager.Instance.WanderingHorde.ForceSpawnWanderingHorde(feral);
                message = "Wandering Hordes spawning for all groups.";

                return false;
            }

            public override (string name, bool optional)[] GetArgs()
            {
                return new (string name, bool optional)[]
                {
                    ("feral", true)
                };
            }

            public override string GetDescription()
            {
                return "Spawns a wandering horde for all groups on the server. Non-Feral unless feral argument is specified (optional).";
            }
        }

        class ResetExecutableCommand : ExecutableSubcommandBase
        {
            public ResetExecutableCommand() : base("reset")
            {
            }

            public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
            {
                var wanderingHorde = ImprovedHordesManager.Instance.WanderingHorde;

                wanderingHorde.spawner.StopAllSpawning();
                wanderingHorde.DisbandAllWanderingHordes();
                wanderingHorde.schedule.Reset();

                message = "Wandering Horde weekly schedule reset.";
                return false;
            }

            public override (string name, bool optional)[] GetArgs()
            {
                return null;
            }

            public override string GetDescription()
            {
                return "Resets the weekly schedule for the wandering hordes.";
            }
        }

        class ShowExecutableCommand : ExecutableSubcommandBase
        {
            public ShowExecutableCommand() : base("show")
            {
            }

            public override bool Execute(List<string> args, CommandSenderInfo _senderInfo, ref string message)
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

                message = builder.ToString();
                return false;
            }

            public override (string name, bool optional)[] GetArgs()
            {
                return null;
            }

            public override string GetDescription()
            {
                return "Shows information regarding the weekly wandering horde schedule.";
            }
        }
    }
}