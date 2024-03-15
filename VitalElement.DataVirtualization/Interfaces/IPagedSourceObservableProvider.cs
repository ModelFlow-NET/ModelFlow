namespace VitalElement.VirtualizingCollection.Interfaces
{
    using System.Collections.Specialized;

    public interface IPagedSourceObservableProvider<T> : IPagedSourceProvider<T>, INotifyCollectionChanged,
        IEditableProvider<T>
    {
    }
}