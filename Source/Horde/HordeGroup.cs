using System.Collections.Generic;

namespace ImprovedHordes.Horde
{
    public sealed class HordeGroup
    {
        public readonly string name;
        public readonly List<HordeGroupEntity> entities = new List<HordeGroupEntity>();

        public readonly RuntimeEval<HashSet<int>> PrefWeekDays;
        public readonly RuntimeEval<int> MaxWeeklyOccurances;

        public HordeGroup(string name, RuntimeEval<HashSet<int>> prefWeekDays, RuntimeEval<int> maxWeeklyOccurances)
        {
            this.name = name;
            this.PrefWeekDays = prefWeekDays;
            this.MaxWeeklyOccurances = maxWeeklyOccurances;
        }
    }

    public sealed class HordeGroupEntity
    {
        public readonly GS gs;

        public readonly string name;
        public readonly string group;

        public readonly RuntimeEval<int> minCount;
        public readonly RuntimeEval<int> maxCount;

        public HordeGroupEntity(GS gs, string name, string group, RuntimeEval<int> minCount, RuntimeEval<int> maxCount)
        {
            this.gs = gs;
            this.name = name;
            this.group = group;
            this.minCount = minCount;
            this.maxCount = maxCount;
        }
    }

    public class GS
    {
        public readonly RuntimeEval<int> min;
        public readonly RuntimeEval<int> max;
        public readonly RuntimeEval<int> countDecGS;

        public readonly RuntimeEval<float> countIncPerGS;
        public readonly RuntimeEval<float> countDecPerPostGS;

        public GS() { }

        public GS(RuntimeEval<int> min, RuntimeEval<int> max, RuntimeEval<int> countDecGS, RuntimeEval<float> countIncPerGS, RuntimeEval<float> countDecPerPostGS)
        {
            this.min = min;
            this.max = max;
            this.countDecGS = countDecGS;
            this.countIncPerGS = countIncPerGS;
            this.countDecPerPostGS = countDecPerPostGS;
        }
    }
}
