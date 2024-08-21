namespace ModelFlow.DataVirtualization.Interfaces
{
    internal interface IBaseSourceProvider : ISynchronized
    {
        void OnReset(int count);
    }
}