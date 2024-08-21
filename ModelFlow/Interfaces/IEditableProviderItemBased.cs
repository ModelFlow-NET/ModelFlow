namespace ModelFlow.DataVirtualization.Interfaces
{
    internal interface IEditableProviderItemBased<in T> : IEditableProvider<T>
    {
        int OnRemove(T item, object timestamp);
        int OnReplace(T oldItem, T newItem, object timestamp);
    }
}