namespace ModelFlow.DataVirtualization.Interfaces
{
    internal interface ISynchronized
    {
        bool IsSynchronized { get; }
        
        object SyncRoot { get; }
    }
}