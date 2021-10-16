using System.Collections.Generic;

namespace ImprovedHordes.Horde
{
    public sealed class HordeGroup
    {
        public readonly string name;
        public readonly List<HordeGroupEntity> entities = new List<HordeGroupEntity>();

        public readonly RuntimeEval.Value<HashSet<int>> PrefWeekDays;
        public readonly RuntimeEval.Value<int> MaxWeeklyOccurances;

        public HordeGroup(string name, RuntimeEval.Value<HashSet<int>> prefWeekDays, RuntimeEval.Value<int> maxWeeklyOccurances)
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

        public readonly RuntimeEval.Value<float> chance;

        public readonly RuntimeEval.Value<int> minCount;
        public readonly RuntimeEval.Value<int> maxCount;

        public HordeGroupEntity(GS gs, string name, string group, RuntimeEval.Value<float> chance, RuntimeEval.Value<int> minCount, RuntimeEval.Value<int> maxCount)
        {
            this.gs = gs;
            this.name = name;
            this.group = group;
            this.chance = chance;
            this.minCount = minCount;
            this.maxCount = maxCount;
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
}
