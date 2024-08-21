namespace ModelFlow.DataVirtualization.Interfaces
{
    internal interface IRepeatingVirtualizationAction
    {
        bool IsDueToRun();
        bool KeepInActionsList();
    }
}