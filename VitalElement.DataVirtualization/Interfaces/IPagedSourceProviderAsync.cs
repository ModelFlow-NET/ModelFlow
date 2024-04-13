namespace VitalElement.DataVirtualization.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pageing;

    internal interface IPagedSourceProviderAsync<T> :  IBaseSourceProvider
    {
        Task<bool> ContainsAsync(T item);

        Task<int> GetCountAsync();

        Task<IEnumerable<T>> GetItemsAtAsync(int offset, int count);

        T GetPlaceHolder(int index, int page, int offset);

        Task<int> IndexOfAsync(T item);
    }
}