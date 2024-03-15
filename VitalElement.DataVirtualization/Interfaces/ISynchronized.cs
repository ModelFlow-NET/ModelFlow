namespace VitalElement.DataVirtualization.Interfaces
{
    public interface ISynchronized
    {
        bool IsSynchronized { get; }
        
        object SyncRoot { get; }
    }
}