namespace VitalElement.DataVirtualization.Interfaces
{
    using System.Collections.Generic;
    using Pageing;

    public interface IPagedSourceProvider<T> : IBaseSourceProvider<T>
    {
        int Count { get; }
        bool Contains(T item);
        IEnumerable<T> GetItemsAt(int pageoffset, int count);

        int IndexOf(T item);
    }
}