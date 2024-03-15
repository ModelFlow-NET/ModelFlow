namespace VitalElement.DataVirtualization.Interfaces
{
    using System.Threading.Tasks;
    using Pageing;

    public interface IPagedSourceProviderAsync<T> :  IBaseSourceProvider<T>
    {
        Task<bool> ContainsAsync(T item);

        Task<int> GetCountAsync();

        Task<PagedSourceItemsPacket<T>> GetItemsAtAsync(int offset, int count);

        T GetPlaceHolder(int index, int page, int offset);

        Task<int> IndexOfAsync(T item);
    }
}