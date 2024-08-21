namespace ModelFlow.DataVirtualization.Interfaces
{
    using System.Collections.Generic;
    using Pageing;

    internal interface IPagedSourceProvider<T> : IBaseSourceProvider
    {
        int Count { get; }
        bool Contains(T item);
        IEnumerable<T> GetItemsAt(int pageoffset, int count);

        int IndexOf(T item);
    }
}