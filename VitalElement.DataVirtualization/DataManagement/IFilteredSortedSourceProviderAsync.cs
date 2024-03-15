namespace VitalElement.DataVirtualization.DataManagement
{
    public interface IFilteredSortedSourceProviderAsync
    {
        FilterDescriptionList FilterDescriptionList { get; }

        SortDescriptionList SortDescriptionList { get; }
    }
}