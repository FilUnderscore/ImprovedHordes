using System.Collections.Generic;

namespace ImprovedHordes.Core.Abstractions.Random
{
    public interface IRandom
    {
        bool RandomChance(float pct);

        int RandomRange(int maxExclusive);
        T Random<T>(IList<T> collection);
    }
}
