namespace VitalElement.DataVirtualization.DataManagement
{
    using System.Collections.Specialized;

    public class FilterDescriptionList : DescriptionList<FilterDescription>
    {
        public void OnCollectionReset()
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}