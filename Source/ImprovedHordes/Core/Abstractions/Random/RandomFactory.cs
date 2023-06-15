namespace ImprovedHordes.Core.Abstractions.Random
{
    public interface IRandomFactory<T> where T : IRandom
    {
        T CreateRandom(int seed);
        T GetSharedRandom();

        void FreeRandom(T random);
    }
}
