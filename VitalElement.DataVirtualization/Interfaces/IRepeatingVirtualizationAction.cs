namespace VitalElement.VirtualizingCollection.Interfaces
{
    public interface IRepeatingVirtualizationAction
    {
        bool IsDueToRun();
        bool KeepInActionsList();
    }
}