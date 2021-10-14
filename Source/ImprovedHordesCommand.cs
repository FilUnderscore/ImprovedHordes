using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImprovedHordes.Horde;

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
                        if (_params[1].EqualsCaseInsensitive("spawn"))
                        {
                            ImprovedHordesMod.manager.wanderingHorde.SpawnWanderingHordes();

                            SingletonMonoBehaviour<SdtdConsole>.Instance.Output("[Improved Hordes] Wandering Hordes spawning for all groups.");
                        }
                        else if (_params[1].EqualsCaseInsensitive("reset"))
                        {
                            var nextResetTime = ImprovedHordesMod.manager.wanderingHorde.GenerateNewResetTime();
                            ImprovedHordesMod.manager.wanderingHorde.GenerateNewSchedule(nextResetTime);

                            SingletonMonoBehaviour<SdtdConsole>.Instance.Output("[Improved Hordes] Wandering Horde weekly schedule reset.");
                        }
                        else if (_params[1].EqualsCaseInsensitive("show"))
                        {
                            var horde = ImprovedHordesMod.manager.wanderingHorde.hordes;
                            var schedule = horde.schedule;

                            StringBuilder builder = new StringBuilder();
                            builder.AppendLine("Constants:");
                            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordes.HOURS_TO_FIRST_OCCURANCE_MIN), WanderingHordes.HOURS_TO_FIRST_OCCURANCE_MIN));
                            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordes.HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX), WanderingHordes.HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX));
                            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordes.MAX_OCCURANCES), WanderingHordes.MAX_OCCURANCES));
                            builder.AppendLine(String.Format("{0}: {1}", nameof(WanderingHordes.HOURS_APART_MIN), WanderingHordes.HOURS_APART_MIN));
                            builder.AppendLine("");
                            builder.AppendLine("Schedule:");
                            foreach (var occurance in schedule.occurances)
                            {
                                var number = occurance.Key;
                                var worldTime = occurance.Value.worldTime;
                                var feral = occurance.Value.feral;

                                var (Days, Hours, Minutes) = GameUtils.WorldTimeToElements(worldTime);
                                builder.AppendLine(String.Format("- {0} at Day {1} {2:D2} {3:D2} ({4})", number + 1, Days, Hours, Minutes, feral ? "Feral" : "Not Feral"));
                            }

                            builder.AppendLine("");

                            if (horde.state == WanderingHordes.EHordeState.Finished)
                            {
                                if (schedule.currentOccurance < schedule.occurances.Count)
                                    builder.AppendLine(String.Format("Next Occurance {0} ", schedule.currentOccurance + 1));
                                else
                                    builder.AppendLine("No more occurances this week.");
                            }
                            else
                            {
                                builder.AppendLine(String.Format("Current Occurance: {0}", schedule.currentOccurance + 1));
                                builder.AppendLine(String.Format("State: {0}", schedule.currentOccurance + 1, nameof(horde.state)));
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
