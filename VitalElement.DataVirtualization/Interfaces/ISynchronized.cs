namespace VitalElement.DataVirtualization.Interfaces
{
    internal interface ISynchronized
    {
        bool IsSynchronized { get; }
        
        object SyncRoot { get; }
    }
}