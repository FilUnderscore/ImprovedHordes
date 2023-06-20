namespace ImprovedHordes.Core.Abstractions.Data
{
    public interface ISaveable<T> where T : IData
    {
        T GetData();
    }
}
