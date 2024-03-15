namespace VitalElement.VirtualizingCollection.Management
{
    public interface IFilteredSortedSourceProviderAsync
    {
        FilterDescriptionList FilterDescriptionList { get; }

        SortDescriptionList SortDescriptionList { get; }
    }
}