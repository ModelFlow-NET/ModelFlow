namespace VitalElement.DataVirtualization.Interfaces
{
    internal interface IRepeatingVirtualizationAction
    {
        bool IsDueToRun();
        bool KeepInActionsList();
    }
}