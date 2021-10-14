using System;
using System.Collections.Generic;
using System.IO;

namespace ImprovedHordes.Horde
{
    public class WanderingHordes
    {
        public const int HOURS_TO_FIRST_OCCURANCE_MIN = 0; // 24 hr time
        public const int HOURS_IN_WEEK_FOR_LAST_OCCURANCE_MAX = 6 * 24 + 12; // 24 hr time

        public const int MIN_OCCURANCES = 2;
        public const int MAX_OCCURANCES = 5;
        public const int HOURS_APART_MIN = 6;

        public EHordeState state = EHordeState.Finished;

        public Schedule schedule = new Schedule();
        public List<WanderingHorde> hordes = new List<WanderingHorde>();

        public struct Occurance
        {
            public readonly ulong worldTime;
            public readonly bool feral;

            public Occurance(ulong worldTime, bool feral)
            {
                this.worldTime = worldTime;
                this.feral = feral;
            }
        }

        public class Schedule
        {
            public ulong nextResetTime = 0UL;
            public int currentOccurance = 0;

            public Dictionary<int, Occurance> occurances = new Dictionary<int, Occurance>();
            public Dictionary<int, Dictionary<string, int>> previousHordeGroupsForPlayers = new Dictionary<int, Dictionary<string, int>>();

            public void Load(BinaryReader reader)
            {
                this.nextResetTime = reader.ReadUInt64();
                this.currentOccurance = reader.ReadInt32();

                occurances.Clear(); // Clear if not empty.
                int occurancesSize = reader.ReadInt32();
                for(int i = 0; i < occurancesSize; i++)
                {
                    int occurance = reader.ReadInt32();
                    ulong occuranceWorldTime = reader.ReadUInt64();
                    bool feral = reader.ReadBoolean();

                    occurances.Add(occurance, new Occurance(occuranceWorldTime, feral));
                }

                previousHordeGroupsForPlayers.Clear();
                int previousHordeGroupsForPlayersSize = reader.ReadInt32();
                for(int i = 0; i < previousHordeGroupsForPlayersSize; i++)
                {
                    int playerId = reader.ReadInt32();

                    previousHordeGroupsForPlayers.Add(playerId, new Dictionary<string, int>());
                    int groupsSize = reader.ReadInt32();
                    
                    for(int j = 0; j < groupsSize; j++)
                    {
                        string group = reader.ReadString();
                        int count = reader.ReadInt32();
                        previousHordeGroupsForPlayers[playerId].Add(group, count);
                    }
                }
            }

            public int GetWeeklyOccurancesForPlayer(EntityPlayer player, HordeGroup group)
            {
                if (!previousHordeGroupsForPlayers.ContainsKey(player.entityId))
                    return 0;
                
                if (!previousHordeGroupsForPlayers[player.entityId].ContainsKey(group.name))
                    return 0;

                return previousHordeGroupsForPlayers[player.entityId][group.name];
            }

            public void Save(BinaryWriter writer)
            {
                writer.Write(this.nextResetTime);
                writer.Write(this.currentOccurance);

                writer.Write(this.occurances.Count);
                foreach(var occurance in this.occurances)
                {
                    writer.Write(occurance.Key);
                    writer.Write(occurance.Value.worldTime);
                    writer.Write(occurance.Value.feral);
                }

                writer.Write(this.previousHordeGroupsForPlayers.Count);
                foreach(var previousHordeGroupsForPlayer in previousHordeGroupsForPlayers)
                {
                    writer.Write(previousHordeGroupsForPlayer.Key);
                    foreach(var group in previousHordeGroupsForPlayer.Value)
                    {
                        writer.Write(group.Key);
                        writer.Write(group.Value);
                    }
                }
            }
        }

        public enum EHordeState
        {
            Spawning,
            StillAlive,
            Finished
        }
    }
}
