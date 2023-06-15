using System.Collections.Generic;

namespace ImprovedHordes.Core.Abstractions.Random
{
    public interface IRandom
    {
        int RandomRange(int maxExclusive);
        T Random<T>(IList<T> collection);
    }
}
