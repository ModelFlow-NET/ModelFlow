namespace VitalElement.DataVirtualization.Interfaces
{
    public interface IBaseSourceProvider<T> : ISynchronized
    {
        void OnReset(int count);
    }
}