namespace VitalElement.DataVirtualization.Management
{
    public interface IFilteredSortedSourceProviderAsync
    {
        FilterDescriptionList FilterDescriptionList { get; }

        SortDescriptionList SortDescriptionList { get; }
    }
}