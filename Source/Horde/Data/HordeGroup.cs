using System.Collections.Generic;

namespace ImprovedHordes.Horde.Data
{
    public sealed class HordeGroup
    {
        public readonly HordeGroupList list;
        public readonly string name;
        public readonly List<HordeGroupEntity> entities = new List<HordeGroupEntity>();

        public readonly RuntimeEval.Value<float> Weight;
        public readonly RuntimeEval.Value<HashSet<int>> PrefWeekDays;
        public readonly RuntimeEval.Value<int> MaxWeeklyOccurrences;

        public readonly string parent;
        public List<HordeGroup> children;

        public HordeGroup(HordeGroupList list, string parent, string name, RuntimeEval.Value<float> weight, RuntimeEval.Value<HashSet<int>> prefWeekDays, RuntimeEval.Value<int> maxWeeklyOccurrences)
        {
            this.list = list;
            this.parent = parent;
            this.name = name;
            this.Weight = weight;
            this.PrefWeekDays = prefWeekDays;
            this.MaxWeeklyOccurrences = maxWeeklyOccurrences;
        }

        public HordeGroup GetParent()
        {
            return list.hordes[parent];
        }

        public List<HordeGroup> GetChildren()
        {
            return this.children;
        }
    }

    public sealed class HordeGroupEntity
    {
        public readonly GS gs;

        public readonly string name;
        public readonly string group;
        public readonly string horde;

        public readonly RuntimeEval.Value<HashSet<string>> biomes;
        
        public readonly RuntimeEval.Value<float> chance;

        public readonly RuntimeEval.Value<int> minCount;
        public readonly RuntimeEval.Value<int> maxCount;
        
        public readonly RuntimeEval.Value<ETimeOfDay> timeOfDay;

        public HordeGroupEntity(GS gs, string name, string group, string horde, RuntimeEval.Value<HashSet<string>> biomes, RuntimeEval.Value<float> chance, RuntimeEval.Value<int> minCount, RuntimeEval.Value<int> maxCount, RuntimeEval.Value<ETimeOfDay> timeOfDay)
        {
            this.gs = gs;
            this.name = name;
            this.group = group;
            this.horde = horde;
            this.biomes = biomes;
            this.chance = chance;
            this.minCount = minCount;
            this.maxCount = maxCount;
            this.timeOfDay = timeOfDay;
        }
    }

    public class GS
    {
        public readonly RuntimeEval.Value<int> min;
        public readonly RuntimeEval.Value<int> max;
        public readonly RuntimeEval.Value<int> countDecGS;

        public readonly RuntimeEval.Value<float> countIncPerGS;
        public readonly RuntimeEval.Value<float> countDecPerPostGS;

        public GS() { }

        public GS(RuntimeEval.Value<int> min, RuntimeEval.Value<int> max, RuntimeEval.Value<int> countDecGS, RuntimeEval.Value<float> countIncPerGS, RuntimeEval.Value<float> countDecPerPostGS)
        {
            this.min = min;
            this.max = max;
            this.countDecGS = countDecGS;
            this.countIncPerGS = countIncPerGS;
            this.countDecPerPostGS = countDecPerPostGS;
        }
    }

    public enum ETimeOfDay
    {
        Anytime,
        Day,
        Night
    }
}
