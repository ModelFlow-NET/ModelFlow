namespace VitalElement.DataVirtualization.Interfaces
{
    public interface IBaseSourceProvider : ISynchronized
    {
        void OnReset(int count);
    }
}