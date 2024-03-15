namespace VitalElement.VirtualizingCollection.Interfaces
{
    using Pageing;

    public interface IPagedSourceProvider<T> : IBaseSourceProvider<T>
    {
        int Count { get; }
        bool Contains(T item);
        PagedSourceItemsPacket<T> GetItemsAt(int pageoffset, int count);

        int IndexOf(T item);
    }
}