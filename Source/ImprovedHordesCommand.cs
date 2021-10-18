using System;
using System.Collections.Generic;
using System.Text;

using ImprovedHordes.Horde;
using ImprovedHordes.Horde.Wandering;

namespace ImprovedHordes
{
    class ImprovedHordeCommand : ConsoleCmdAbstract
    {
        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            if (_params.Count >= 1)
            {
                if (_params[0].EqualsCaseInsensitive("wandering"))
                {
                    if (_params.Count >= 2)
                    {
                        var wanderingHorde = HordeManager.Instance.WanderingHorde;

                        if (_params[1].EqualsCaseInsensitive("spawn"))
                        {
                            // TODO.
                            //ImprovedHordesMod.manager.wanderingHorde.SpawnWanderingHordes();
                            wanderingHorde.ForceSpawnWanderingHorde();

                            SingletonMonoBehaviour<SdtdConsole>.Instance.Output("[Improved Hordes] Wandering Hordes spawning for all groups.");
                        }
                        else if (_params[1].EqualsCaseInsensitive("reset"))
                        {
                            wanderingHorde.state = WanderingHorde.EHordeState.Finished;
                            var nextResetTime = wanderingHorde.GenerateNewResetTime();
                            wanderingHorde.GenerateNewSchedule(nextResetTime);

                            SingletonMonoBehaviour<SdtdConsole>.Instance.Output("[Improved Hordes] Wandering Horde weekly schedule reset.");
                        }
                        else if (_params[1].EqualsCaseInsensitive("show"))
                        {
                            var schedule = wanderingHorde.schedule;

                            StringBuilder builder = new StringBuilder();
                            builder.AppendLine("Constants:");
                            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHorde.HOURS_TO_FIRST_OCCURANCE_MIN), WanderingHorde.HOURS_TO_FIRST_OCCURANCE_MIN));
                            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHorde.HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX), WanderingHorde.HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX));
                            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHorde.MAX_OCCURANCES), WanderingHorde.MAX_OCCURANCES));
                            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHorde.HOURS_APART_MIN), WanderingHorde.HOURS_APART_MIN));
                            builder.AppendLine("");
                            builder.AppendLine("Schedule:");
                            for (int i = 0; i < schedule.occurances.Count; i++)
                            {
                                var occurance = schedule.occurances[i];
                                var worldTime = occurance.worldTime;
                                var feral = occurance.feral;

                                var (Days, Hours, Minutes) = GameUtils.WorldTimeToElements(worldTime);
                                builder.AppendLine(String.Format("- {0} at Day {1} {2:D2} {3:D2} ({4})", i + 1, Days, Hours, Minutes, feral ? "Feral" : "Not Feral"));
                            }

                            builder.AppendLine("");

                            if (wanderingHorde.state == WanderingHorde.EHordeState.Finished)
                            {
                                if (schedule.currentOccurance < schedule.occurances.Count)
                                    builder.AppendLine(String.Format("Next Occurance {0} ", schedule.currentOccurance + 1));
                                else
                                    builder.AppendLine("No more occurances this week.");
                            }
                            else
                            {
                                builder.AppendLine(String.Format("Current Occurance: {0}", schedule.currentOccurance + 1));
                                builder.AppendLine(String.Format("State: {0}", schedule.currentOccurance + 1, nameof(wanderingHorde.state)));
                            }

                            builder.AppendLine("");

                            var resetWorldTime = schedule.nextResetTime;
                            var resetWorldTimeElements = GameUtils.WorldTimeToElements(resetWorldTime);
                            builder.AppendLine(String.Format("Next reset at Day {0} {1:D2} {2:D2}", resetWorldTimeElements.Days, resetWorldTimeElements.Hours, resetWorldTimeElements.Minutes));

                            SingletonMonoBehaviour<SdtdConsole>.Instance.Output(builder.ToString());
                        }
                    }
                    else
                    {
                        SingletonMonoBehaviour<SdtdConsole>.Instance.Output("No sub command given.");
                    }
                }
            }
            else
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("No sub command given.");
            }
        }

        public override string[] GetCommands()
        {
            return new string[] { "improvedhordes", "ih" };        
        }

        public override string GetDescription()
        {
            return "Execute a function from the Improved Hordes Server Mod. help improvedhordes - for more information.";
        }

        public override string GetHelp()
        {
            return "Commands: \nimprovedhordes wandering spawn - Spawns a wandering horde for all groups on the server."
                + "\nimprovedhordes wandering resetschedule - Resets the weekly schedule for the wandering hordes."
                + "\nimprovedhordes wandering show - Shows information regarding the weekly wandering horde schedule.";
        }
    }
}
