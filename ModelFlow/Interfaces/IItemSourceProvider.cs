namespace ModelFlow.DataVirtualization.Interfaces
{
    internal interface IItemSourceProvider<T> : IBaseSourceProvider
    {
        bool Contains(T item);
        T GetAt(int index, object voc);
        int GetCount(bool asyncOk);

        int IndexOf(T item);
    }
}