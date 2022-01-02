using System;
using System.Collections.Generic;
using System.IO;

using ImprovedHordes.Horde.Data;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Wandering
{
    public class WanderingHordeSchedule
    {
        private int s_days_per_wandering_week,
            s_hrs_in_week_to_first_occurrence,
            s_hrs_in_week_for_last_occurrence_max,
            s_min_hrs_between_occurrences,
            s_min_occurrences,
            s_max_occurrences;

        private float s_feral_horde_chance;

        public int DAYS_PER_RESET
        {
            get
            {
                return s_days_per_wandering_week;
            }
        }

        public int HOURS_TO_FIRST_OCCURRENCE_MIN
        {
            get
            {
                return s_hrs_in_week_to_first_occurrence;
            }
        }

        public int HOURS_IN_WEEK_FOR_LAST_OCCURRENCE_MAX
        {
            get
            {
                return s_hrs_in_week_for_last_occurrence_max;
            }
        }

        public int MIN_OCCURRENCES
        {
            get
            {
                return s_min_occurrences;
            }
        }

        public int MAX_OCCURRENCES
        {
            get
            {
                return s_max_occurrences;
            }
        }
        
        public int MIN_HRS_BETWEEN_OCCURRENCES
        {
            get
            {
                return s_min_hrs_between_occurrences;
            }
        }

        public float FERAL_HORDE_CHANCE
        {
            get
            {
                return s_feral_horde_chance;
            }
        }

        public ulong nextResetTime = 0UL;
        public int currentOccurrence = 0;

        public readonly List<Occurrence> occurrences = new List<Occurrence>();
        public readonly Dictionary<int, Dictionary<string, int>> previousHordeGroupsForPlayers = new Dictionary<int, Dictionary<string, int>>();

        private readonly WanderingHordeManager manager;

        public WanderingHordeSchedule(WanderingHordeManager manager)
        {
            this.manager = manager;
        }

        public void Shutdown()
        {
            RuntimeEval.Registry.DeregisterVariable("week");
            RuntimeEval.Registry.DeregisterVariable("weekDay");
        }

        public void ReadSettings(Settings settings)
        {
            this.s_days_per_wandering_week = settings.GetInt("days_per_wandering_week", 1, false, 7);

            this.s_hrs_in_week_to_first_occurrence = settings.GetInt("hrs_in_week_to_first_occurrence", 0, false, 0);
            this.s_hrs_in_week_for_last_occurrence_max = settings.GetInt("hrs_in_week_for_last_occurrence_max", this.s_days_per_wandering_week * 24, true, 156); // TODO account for morning of first day of new week, otherwise a bug could occur.
            this.s_min_hrs_between_occurrences = settings.GetInt("min_hrs_between_occurrences", 0, false, 6);

            this.s_min_occurrences = settings.GetInt("min_occurrences", 0, false, 2);
            this.s_max_occurrences = settings.GetInt("max_occurrences", this.s_min_occurrences + 1, false, this.s_min_occurrences + 3);

            this.s_feral_horde_chance = settings.GetFloat("feral_horde_chance", 0.0f, false, 0.5f);

            if(this.s_feral_horde_chance > 1.0f)
            {
                Warning("[Wandering Horde] Feral horde chance greater than 1. Setting to 1.");
                this.s_feral_horde_chance = 1.0f;
            }    
        }

        public void SetGameVariables()
        {
            RuntimeEval.Registry.RegisterVariable("week", this.GetCurrentWeek);
            RuntimeEval.Registry.RegisterVariable("weekDay", this.GetCurrentWeekDay);
        }

        public void Load(BinaryReader reader)
        {
            this.nextResetTime = reader.ReadUInt64();
            this.currentOccurrence = reader.ReadInt32();

            occurrences.Clear(); // Clear if not empty.
            int occurrencesSize = reader.ReadInt32();
            for (int i = 0; i < occurrencesSize; i++)
            {
                ulong occurrenceWorldTime = reader.ReadUInt64();
                bool feral = reader.ReadBoolean();

                occurrences.Add(new Occurrence(occurrenceWorldTime, feral));
            }

            previousHordeGroupsForPlayers.Clear();
            int previousHordeGroupsForPlayersSize = reader.ReadInt32();
            for (int i = 0; i < previousHordeGroupsForPlayersSize; i++)
            {
                int playerId = reader.ReadInt32();

                previousHordeGroupsForPlayers.Add(playerId, new Dictionary<string, int>());
                int groupsSize = reader.ReadInt32();

                for (int j = 0; j < groupsSize; j++)
                {
                    string group = reader.ReadString();
                    int count = reader.ReadInt32();
                    previousHordeGroupsForPlayers[playerId].Add(group, count);
                }
            }
        }

        public int GetAverageWeeklyOccurrencesForGroup(PlayerHordeGroup group, HordeGroup hordeGroup)
        {
            float total = 0.0f;
            foreach (var player in group.members)
            {
                total += GetWeeklyOccurrencesForPlayer(player, hordeGroup);
            }

            total /= group.members.Count;

            return (int)Math.Ceiling(total);
        }

        public int GetWeeklyOccurrencesForPlayer(EntityPlayer player, HordeGroup group)
        {
            if (!previousHordeGroupsForPlayers.ContainsKey(player.entityId))
                return 0;

            if (!previousHordeGroupsForPlayers[player.entityId].ContainsKey(group.name))
                return 0;

            return previousHordeGroupsForPlayers[player.entityId][group.name];
        }

        public void AddWeeklyOccurrencesForGroup(HashSet<EntityPlayer> players, HordeGroup group)
        {
            foreach (var player in players)
            {
                AddWeeklyOccurrencesForPlayer(player, group);
            }
        }

        public void AddWeeklyOccurrencesForPlayer(EntityPlayer player, HordeGroup group)
        {
            if (!previousHordeGroupsForPlayers.ContainsKey(player.entityId))
                previousHordeGroupsForPlayers.Add(player.entityId, new Dictionary<string, int>());

            var dict = previousHordeGroupsForPlayers[player.entityId];

            if (!dict.ContainsKey(group.name))
                dict.Add(group.name, 0);

            dict[group.name]++;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(this.nextResetTime);
            writer.Write(this.currentOccurrence);

            writer.Write(this.occurrences.Count);
            foreach (var occurrence in this.occurrences)
            {
                writer.Write(occurrence.worldTime);
                writer.Write(occurrence.feral);
            }

            writer.Write(this.previousHordeGroupsForPlayers.Count);
            foreach (var previousHordeGroupsForPlayer in previousHordeGroupsForPlayers)
            {
                writer.Write(previousHordeGroupsForPlayer.Key);
                foreach (var group in previousHordeGroupsForPlayer.Value)
                {
                    writer.Write(group.Key);
                    writer.Write(group.Value);
                }
            }
        }

        public Occurrence GetCurrentOccurrence()
        {
            return this.occurrences[this.currentOccurrence];
        }

        public bool IsOccurrenceDue() // Current occurrence
        {
            return this.currentOccurrence < this.occurrences.Count && this.GetWorldTime() >= this.occurrences[this.currentOccurrence].worldTime; 
        }
        public bool IsNextOccurrenceDue() // Current occurrence + 1
        {
            return this.currentOccurrence + 1 < this.occurrences.Count && this.GetWorldTime() >= this.occurrences[this.currentOccurrence + 1].worldTime;
        }

        public bool CheckIfNeedsReset()
        {
            return this.GetWorldTime() >= this.nextResetTime;
        }

        public ulong GetWorldTime()
        {
            return this.manager.manager.World.GetWorldTime();
        }

        /// <summary>
        /// Gets the relative day to the current week, e.g. Day 8 is Day 1 of Week 2. etc.
        /// </summary>
        /// <param name="day">Day in week (1-7).</param>
        /// <returns>Total day accounting for weeks.</returns>
        public int GetTotalDayRelativeToNextWeek(int day)
        {
            int currentDays = GameUtils.WorldTimeToDays(this.GetWorldTime());
            int dayInWeek = (int)Math.Ceiling(currentDays / (float)DAYS_PER_RESET) * DAYS_PER_RESET + day;

            return dayInWeek;
        }

        public int GetTotalDayRelativeToThisWeek(int day)
        {
            int currentDays = GameUtils.WorldTimeToDays(this.GetWorldTime());
            int dayInWeek = (int)Math.Floor((currentDays - 1) / (float)DAYS_PER_RESET) * DAYS_PER_RESET + day;

            return dayInWeek;
        }

        public int GetCurrentWeekDay()
        {
            int totalDay = GameUtils.WorldTimeToDays(this.GetWorldTime());
            int modulo = totalDay % DAYS_PER_RESET;
            return modulo == 0 ? DAYS_PER_RESET : modulo;
        }

        public int GetCurrentWeek()
        {
            int totalDay = GameUtils.WorldTimeToDays(this.GetWorldTime());

            return (int)Math.Floor(totalDay / (float)DAYS_PER_RESET) + 1;
        }

        public ulong GenerateNewResetTime()
        {
            int dayLightLength = GamePrefs.GetInt(EnumGamePrefs.DayLightLength);
            int night = 22;

            if (dayLightLength > night)
                dayLightLength = 22; // Don't allow negative values.

            int nextDayAfterHordeReset = GetTotalDayRelativeToNextWeek(1);

            return GameUtils.DayTimeToWorldTime(nextDayAfterHordeReset, night - dayLightLength, 0);
        }

        public void Reset()
        {
            this.currentOccurrence = 0;
            this.nextResetTime = 0UL;
            this.previousHordeGroupsForPlayers.Clear();
            this.occurrences.Clear();

            ulong nextResetTime = this.GenerateNewResetTime();
#if DEBUG
            var (Days, Hours, Minutes) = GameUtils.WorldTimeToElements(nextResetTime);
            Log("[Wandering Horde] Resetting wandering horde. Next reset at Day {0} {1:D2}:{2:D2}", Days, Hours, Minutes);
#endif

            this.GenerateNewSchedule(nextResetTime);
        }

        public void GenerateNewSchedule(ulong nextResetTime)
        {
            var random = this.manager.manager.Random;

            int maxOccurrences = random.RandomRange(MIN_OCCURRENCES, MAX_OCCURRENCES);

            int possibleOccurrences = 0;
            bool possible = false;
            ulong lastOccurrence = 0UL;
            for (int i = 0; i < maxOccurrences; i++)
            {
                ulong nextOccurrence = GenerateNextOccurrence(i, maxOccurrences, out possible, random, lastOccurrence);
                lastOccurrence = nextOccurrence;

                if (!possible)
                {
                    if (possibleOccurrences == 0)
                        Log("[Wandering Horde] No occurrences will be scheduled for the remainder of the week.");
                    else
                        Log("[Wandering Horde] {0} occurrences out of {1} were scheduled for the week.", possibleOccurrences, maxOccurrences);

                    break;
                }

                bool feral = FERAL_HORDE_CHANCE < 1.0f ? random.RandomRange(0.0f, 1.0f) <= FERAL_HORDE_CHANCE : true;
                occurrences.Add(new Occurrence(nextOccurrence, feral));
                possibleOccurrences++;

#if DEBUG
                var (Days, Hours, Minutes) = GameUtils.WorldTimeToElements(nextOccurrence);
                Log("[Wandering Horde] Occurrence {0} at Day {1} {2}:{3}", i, Days, Hours, Minutes);
#endif
            }

#if DEBUG
            Log("[Wandering Horde] Possible occurrences this week: {0}", possibleOccurrences);
#endif

            this.nextResetTime = nextResetTime;
        }

        public ulong GenerateNextOccurrence(int occurrence, int occurrences, out bool possible, GameRandom random, ulong lastOccurrence)
        {
            var worldTime = this.GetWorldTime();
            var maxWorldTime = GameUtils.DaysToWorldTime(this.GetTotalDayRelativeToThisWeek(1)) + (ulong)(HOURS_IN_WEEK_FOR_LAST_OCCURRENCE_MAX * 1000);

            var deltaWorldTime = maxWorldTime - (lastOccurrence > 0 ? lastOccurrence : worldTime);

            if (occurrence == 0)
            {
                int randomHr = random.RandomRange(HOURS_TO_FIRST_OCCURRENCE_MIN, (int)(Math.Floor((float)(HOURS_IN_WEEK_FOR_LAST_OCCURRENCE_MAX - 12) / 24) / (float)occurrences) * 24 + 12);

                ulong randomHrToWorldTime = (ulong)randomHr * 1000UL + worldTime;

                if (randomHrToWorldTime > maxWorldTime)
                {
                    possible = worldTime <= maxWorldTime;
                    return maxWorldTime - deltaWorldTime / 2;
                }
                else
                {
                    possible = true;
                    return randomHrToWorldTime;
                }
            }
            else
            {
                int hoursApartOccurrencesMin = MIN_HRS_BETWEEN_OCCURRENCES * 1000;

                int minNextOccurance = hoursApartOccurrencesMin;
                int maxNextOccurance = minNextOccurance + (int)((float)deltaWorldTime * ((float)occurrence / (float)occurrences));

                ulong nextOccurance = lastOccurrence + (ulong)random.RandomRange(minNextOccurance, maxNextOccurance);

                possible = nextOccurance <= maxWorldTime;
                return nextOccurance;
            }
        }

        public readonly struct Occurrence
        {
            public readonly ulong worldTime;
            public readonly bool feral;

            public Occurrence(ulong worldTime, bool feral)
            {
                this.worldTime = worldTime;
                this.feral = feral;
            }
        }
    }
}
