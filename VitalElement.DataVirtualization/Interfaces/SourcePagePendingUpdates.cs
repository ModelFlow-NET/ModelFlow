namespace VitalElement.DataVirtualization.Interfaces
{
    using System.Collections.Specialized;

    public class SourcePagePendingUpdates
    {
        public INotifyCollectionChanged Args { get; set; }
        public object UpdatedAt { get; set; }
    }
}