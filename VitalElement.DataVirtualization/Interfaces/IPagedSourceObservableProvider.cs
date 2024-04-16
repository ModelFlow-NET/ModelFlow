namespace VitalElement.DataVirtualization.Interfaces
{
    using System.Collections.Specialized;

    internal interface IPagedSourceObservableProvider<T> : IPagedSourceProvider<T>, INotifyCollectionChanged,
        IEditableProvider<T>
    {
    }
}