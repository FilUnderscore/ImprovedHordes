using ImprovedHordes.Core.Abstractions.Random;
using UnityEngine;

namespace ImprovedHordes.Core.Abstractions.World.Random
{
    public interface IWorldRandom : IRandom
    {
        Vector3 RandomLocation3 { get; }
        Vector2 RandomLocation2 { get; }
    }
}
