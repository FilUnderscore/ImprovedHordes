using System;
using System.Collections.Generic;
using System.IO;

using static ImprovedHordes.Utils.Logger;

namespace ImprovedHordes.Horde.Wandering
{
    public class WanderingHordeSchedule
    {
        // TODO XML Setting.
        public const int HOURS_TO_FIRST_OCCURANCE_MIN = 0; // 24 hr time
        public const int HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX = 6 * 24 + 12; // 24 hr time

        public const int MIN_OCCURANCES = 2;
        public const int MAX_OCCURANCES = 5;
        public const int HOURS_APART_MIN = 6;

        public ulong nextResetTime = 0UL;
        public int currentOccurance = 0;

        public readonly List<Occurance> occurances = new List<Occurance>();
        public readonly Dictionary<int, Dictionary<string, int>> previousHordeGroupsForPlayers = new Dictionary<int, Dictionary<string, int>>();

        private WanderingHordeManager manager;

        public WanderingHordeSchedule(WanderingHordeManager manager)
        {
            this.manager = manager;

            RuntimeEval.Registry.RegisterVariable("week", this.GetCurrentWeek);
            RuntimeEval.Registry.RegisterVariable("weekDay", this.GetCurrentWeekDay);
        }

        public void Load(BinaryReader reader)
        {
            this.nextResetTime = reader.ReadUInt64();
            this.currentOccurance = reader.ReadInt32();

            occurances.Clear(); // Clear if not empty.
            int occurancesSize = reader.ReadInt32();
            for (int i = 0; i < occurancesSize; i++)
            {
                ulong occuranceWorldTime = reader.ReadUInt64();
                bool feral = reader.ReadBoolean();

                occurances.Add(new Occurance(occuranceWorldTime, feral));
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

        public int GetAverageWeeklyOccurancesForGroup(PlayerHordeGroup group, HordeGroup hordeGroup)
        {
            float total = 0.0f;
            foreach (var player in group.members)
            {
                total += GetWeeklyOccurancesForPlayer(player, hordeGroup);
            }

            total /= group.members.Count;

            return (int)Math.Ceiling(total);
        }

        public int GetWeeklyOccurancesForPlayer(EntityPlayer player, HordeGroup group)
        {
            if (!previousHordeGroupsForPlayers.ContainsKey(player.entityId))
                return 0;

            if (!previousHordeGroupsForPlayers[player.entityId].ContainsKey(group.name))
                return 0;

            return previousHordeGroupsForPlayers[player.entityId][group.name];
        }

        public void AddWeeklyOccurancesForGroup(List<EntityPlayer> players, HordeGroup group)
        {
            foreach (var player in players)
            {
                AddWeeklyOccurancesForPlayer(player, group);
            }
        }

        public void AddWeeklyOccurancesForPlayer(EntityPlayer player, HordeGroup group)
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
            writer.Write(this.currentOccurance);

            writer.Write(this.occurances.Count);
            foreach (var occurance in this.occurances)
            {
                writer.Write(occurance.worldTime);
                writer.Write(occurance.feral);
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

        public Occurance GetCurrentOccurance()
        {
            return this.occurances[this.currentOccurance];
        }

        public bool IsOccuranceDue() // Current occurance
        {
            return this.currentOccurance < this.occurances.Count && this.GetWorldTime() >= this.occurances[this.currentOccurance].worldTime; 
        }
        public bool IsNextOccuranceDue() // Current occurance + 1
        {
            return this.currentOccurance + 1 < this.occurances.Count && this.GetWorldTime() >= this.occurances[this.currentOccurance + 1].worldTime;
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
            int dayInWeek = (int)Math.Ceiling(currentDays / (float)7) * 7 + day;

            return dayInWeek;
        }

        public int GetTotalDayRelativeToThisWeek(int day)
        {
            int currentDays = GameUtils.WorldTimeToDays(this.GetWorldTime());
            int dayInWeek = (int)Math.Floor((currentDays - 1) / (float)7) * 7 + day;

            return dayInWeek;
        }

        public int GetCurrentWeekDay()
        {
            int totalDay = GameUtils.WorldTimeToDays(this.GetWorldTime());
            int modulo = totalDay % 7;
            return modulo == 0 ? 7 : modulo;
        }

        public int GetCurrentWeek()
        {
            int totalDay = GameUtils.WorldTimeToDays(this.GetWorldTime());

            return (int)Math.Floor(totalDay / (float)7) + 1;
        }

        public ulong GenerateNewResetTime()
        {
            int dayLightLength = GamePrefs.GetInt(EnumGamePrefs.DayLightLength);
            int night = 22;

            int nextDayAfterHordeReset = GetTotalDayRelativeToNextWeek(1);

            return GameUtils.DayTimeToWorldTime(nextDayAfterHordeReset, night - dayLightLength, 0);
        }

        public void Reset()
        {
            this.currentOccurance = 0;
            this.nextResetTime = 0UL;
            this.previousHordeGroupsForPlayers.Clear();
            this.occurances.Clear();

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

            int maxOccurances = random.RandomRange(MIN_OCCURANCES, MAX_OCCURANCES);

            int possibleOccurances = 0;
            bool possible = false;
            ulong lastOccurance = 0UL;
            for (int i = 0; i < maxOccurances; i++)
            {
                ulong nextOccurance = GenerateNextOccurance(i, maxOccurances, out possible, random, lastOccurance);
                lastOccurance = nextOccurance;

                if (!possible)
                {
                    if (possibleOccurances == 0)
                        Log("[Wandering Horde] No occurances will be scheduled for the remainder of the week.");
                    else
                        Log("[Wandering Horde] {0} occurances out of {1} were scheduled for the week.", possibleOccurances, maxOccurances);

                    break;
                }

                bool feral = random.RandomRange(0, 10) >= 5;
                occurances.Add(new Occurance(nextOccurance, feral));
                possibleOccurances++;

#if DEBUG
                var (Days, Hours, Minutes) = GameUtils.WorldTimeToElements(nextOccurance);
                Log("[Wandering Horde] Occurance {0} at Day {1} {2}:{3}", i, Days, Hours, Minutes);
#endif
            }

#if DEBUG
            Log("[Wandering Horde] Possible occurances this week: {0}", possibleOccurances);
#endif

            this.nextResetTime = nextResetTime;
        }

        public ulong GenerateNextOccurance(int occurance, int occurances, out bool possible, GameRandom random, ulong lastOccurance)
        {
            var worldTime = this.GetWorldTime();
            var maxWorldTime = GameUtils.DaysToWorldTime(this.GetTotalDayRelativeToThisWeek(1)) + HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX * 1000;

            var deltaWorldTime = maxWorldTime - (lastOccurance > 0 ? lastOccurance : worldTime);

            if (occurance == 0)
            {
                int randomHr = random.RandomRange(HOURS_TO_FIRST_OCCURANCE_MIN, (int)(Math.Floor((float)(HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX - 12) / 24) / (float)occurances) * 24 + 12);

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
                ulong nextOccurance = lastOccurance + (deltaWorldTime / (ulong)occurances);
                ulong deltaOccuranceTime = nextOccurance - lastOccurance;
                ulong hoursApartOccurancesMin = HOURS_APART_MIN * 1000UL;

                if (deltaOccuranceTime >= hoursApartOccurancesMin)
                {
                    possible = true;
                    return lastOccurance + (deltaWorldTime / (ulong)occurances);
                }
                else
                {
                    possible = false;
                    return 0UL;
                }
            }
        }

        public readonly struct Occurance
        {
            public readonly ulong worldTime;
            public readonly bool feral;

            public Occurance(ulong worldTime, bool feral)
            {
                this.worldTime = worldTime;
                this.feral = feral;
            }
        }
    }
}
