namespace ModelFlow.DataVirtualization.Interfaces
{
    using System.Collections.Specialized;

    internal class SourcePagePendingUpdates
    {
        public INotifyCollectionChanged Args { get; set; }
        public object UpdatedAt { get; set; }
    }
}