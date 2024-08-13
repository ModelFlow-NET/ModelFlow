namespace VitalElement.DataVirtualization.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal interface IPagedSourceProviderAsync<T> :  IBaseSourceProvider
    {
        void Replace(T old, T newItem);

        Task<bool> ContainsAsync(T item);

        Task<int> GetCountAsync();

        Task<IEnumerable<T>> GetItemsAtAsync(ISourcePage<T> page, int offset, int count, Action? signal);

        T GetPlaceHolder(int index, int page, int offset);

        Task<int> IndexOfAsync(T item);
    }
}