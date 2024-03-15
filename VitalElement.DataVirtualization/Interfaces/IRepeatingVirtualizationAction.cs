namespace VitalElement.DataVirtualization.Interfaces
{
    public interface IRepeatingVirtualizationAction
    {
        bool IsDueToRun();
        bool KeepInActionsList();
    }
}