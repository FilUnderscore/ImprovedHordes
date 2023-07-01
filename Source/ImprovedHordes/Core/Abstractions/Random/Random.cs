using System.Collections.Generic;
using UnityEngine;

namespace ImprovedHordes.Core.Abstractions.Random
{
    public interface IRandom
    {
        float RandomFloat { get; }

        bool RandomChance(float pct);

        int RandomRange(int maxExclusive);
        T Random<T>(IList<T> collection);

        Vector2 RandomOnUnitCircle { get; }
    }
}
